/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBaseGdi;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AasxPluginDocumentShelf
{
	/// <summary>
	/// After many troubles with running the preview service (load or generate preview images
	/// for files within AASX files) within the control, e.g. multiple instances, this is an
	/// approach to create a singleton which is created only once for the overall application.
	/// </summary>
	public class ShelfPreviewService
	{
		//
		// Start of service
		//

		private LogInstance _log = new LogInstance();

		private const int _timerTickMs = 300;
		private System.Timers.Timer _dispatcherTimer = null;

		public void StartOperation(LogInstance log)
		{
			_log = log;

			_dispatcherTimer = new System.Timers.Timer(_timerTickMs);
			_dispatcherTimer.Elapsed += DispatcherTimer_Tick;
			_dispatcherTimer.Enabled = true;
			_dispatcherTimer.Start();
		}

		//
		// Render entities
		//

		/// <summary>
		/// Holds a single entity to render. The link to the (open) AASX package is cleared
		/// as soon as possible to allow GC. The entities can be looked up by package filename
		/// and inner file name.
		/// </summary>
		public class RenderEntity
		{
			public string PackageFn = null;
			public string SupplFn = null;

			public AnyUiBitmapInfo Bitmap = null;

			public AdminShellPackageEnv Package = null;

			public DateTime LastUse = DateTime.Now;

			public RenderEntity(AdminShellPackageEnv package, string supplFn)
			{
				Package = package;
				PackageFn = package?.Filename;
				SupplFn = supplFn;
			}
		}

		/// <summary>
		/// Incoming, from user
		/// </summary>
		protected List<RenderEntity> _toRenderEntities = new List<RenderEntity>();

		/// <summary>
		/// Popped from incoming, already available to be used
		/// </summary>
		protected Dictionary<string, RenderEntity> _renderedEntities = new Dictionary<string, RenderEntity>();

		/// <summary>
		/// Duplicated from <c>_renderedEntities</c>, to be frequently checked to be deleted
		/// </summary>
		protected List<RenderEntity> _toDeleteEntities = new List<RenderEntity>();

		public void Push(RenderEntity ent)
		{
			lock (_toRenderEntities)
			{
				_toRenderEntities.Add(ent);
			}
		}

		public RenderEntity Get(string packageFn, string supplFn)
		{
			if (packageFn == null || supplFn == null)
				return null;
			var key = packageFn.Trim() + "|" + supplFn.Trim();
			if (!_renderedEntities.ContainsKey(key))
				return null;
			var res = _renderedEntities[key];
			res.LastUse = DateTime.Now;
			return res;
		}

		protected void PushRendered(RenderEntity ent)
		{
			if (ent?.PackageFn == null || ent?.SupplFn == null)
				return;

			// to output
			var key = ent.PackageFn.Trim() + "|" + ent.SupplFn.Trim();
			ent.LastUse = DateTime.Now;			
			_renderedEntities.Add(key, ent);

			// also keep track to be deleted
			_toDeleteEntities.Add(ent);
		}

		//
		// Service
		//

		private object mutexDocEntitiesInPreview = new object();
		private int numDocEntitiesInPreview = 0;
		
		private const int maxDocEntitiesInPreview = 3;

		private const int maxMinutesToRetain = 3;
		private const int maxRenderedEntitiesToKeep = 3;

		private bool _inDispatcherTimer = false;

		private void DispatcherTimer_Tick(object sender, EventArgs e)
		{
			// access
			if (_toRenderEntities == null || _renderedEntities == null || _inDispatcherTimer)
				return;

			_inDispatcherTimer = true;

			// each tick check for one image, if a preview shall be done
			if (_toRenderEntities.Count > 0)
			{
				// pop
				RenderEntity ent = null;
				lock (_toRenderEntities)
				{
					ent = _toRenderEntities[0];
					_toRenderEntities.RemoveAt(0);
				}

				// check, if valid entity is already in the output
				var exist = Get(ent?.PackageFn, ent?.SupplFn);
				if (exist != null)
					return;

				// no, prepare
				try
				{
					// temp input
					if (ent?.Package != null && ent.PackageFn != null && ent.SupplFn != null)
					{
						// try check if Magick.NET library is available
						var thumbBI = AnyUiGdiHelper.MakePreviewFromPackageOrUrl(ent.Package, ent.SupplFn);
						if (thumbBI != null)
						{
							ent.Bitmap = thumbBI;
							PushRendered(ent);
						}
						else
						{
							//
							// OLD way: use external program to convert
							//

							// makes only sense under Windows
							if (OperatingSystemHelper.IsWindows())
							{
								// from package?
								var inputFn = ent.SupplFn;
								if (inputFn.StartsWith("/"))
									inputFn = ent.Package?.MakePackageFileAvailableAsTempFile(inputFn);

								// temp output
								string outputFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".png");

								// start process
								string arguments = string.Format(
									"-flatten -density 75 \"{0}\"[0] \"{1}\"", inputFn, outputFn);
								string exeFn = System.IO.Path.Combine(
									System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
									"convert.exe");

								var startInfo = new ProcessStartInfo(exeFn, arguments)
								{
									WindowStyle = ProcessWindowStyle.Hidden
								};

								var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

								RenderEntity lambdaEntity = ent;
								string inputFnBuffer = inputFn;
								string outputFnBuffer = outputFn;
								process.Exited += (sender2, args) =>
								{
									// release number of parallel processes
									lock (mutexDocEntitiesInPreview)
									{
										numDocEntitiesInPreview--;
									}

									// try load
									try
									{ 
										lambdaEntity.Bitmap = AnyUiGdiHelper.CreateAnyUiBitmapInfo(outputFnBuffer);
									}
									catch (Exception ex)
									{
										LogInternally.That.SilentlyIgnoredError(ex);
									}

									// try delete
									try
									{
										System.IO.File.Delete(inputFnBuffer);
										System.IO.File.Delete(outputFnBuffer);
									}
									catch (Exception ex)
									{
										LogInternally.That.SilentlyIgnoredError(ex);
									}
								};

								try
								{
									process.Start();
								}
								catch (Exception ex)
								{
									AdminShellNS.LogInternally.That.Error(
										ex, $"Failed to start the process: {startInfo.FileName} " +
											$"with arguments {string.Join(" ", startInfo.Arguments)}");
								}

								// limit the number of parallel executions
								lock (mutexDocEntitiesInPreview)
								{
									numDocEntitiesInPreview++;
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
				}
			}

			// check to release ressources?
			if (_toDeleteEntities != null
				&& _toDeleteEntities.Count >= maxRenderedEntitiesToKeep
				&& (DateTime.Now - _toDeleteEntities[0].LastUse).TotalMinutes >= maxMinutesToRetain)
			{
				// remove
				var toRem = _toDeleteEntities[0];
				
				if (_renderedEntities != null
					&& _renderedEntities.ContainsValue(toRem))
				{
					var item = _renderedEntities.FirstOrDefault(kvp => kvp.Value == toRem);
					_renderedEntities.Remove(item.Key);
				}

				_toDeleteEntities.RemoveAt(0);
			}


			// release mutex
			_inDispatcherTimer = false;

		}
	}
}
