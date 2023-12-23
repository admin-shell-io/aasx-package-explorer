/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasCore.Samm2_2_0;
using AasxAmlImExport;
using AasxCompatibilityModels;
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Xaml;
using VDS.RDF.Parsing;
using VDS.RDF;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Aas = AasCore.Aas3_0;
using Samm = AasCore.Samm2_2_0;
using System.Text.RegularExpressions;
using System.Runtime.Intrinsics.X86;
using Lucene.Net.Tartarus.Snowball.Ext;
using Lucene.Net.Util;
using System.Runtime.Serialization;
using J2N.Text;
using Lucene.Net.Codecs;
using VDS.RDF.Writing;
using AngleSharp.Text;
using System.Web.Services.Description;
using static AasxPackageLogic.DispEditHelperBasics;
using System.Collections;
using static Lucene.Net.Documents.Field;
using static AasxPackageLogic.DispEditHelperMiniModules;

namespace AasxPackageLogic
{
	/// <summary>
	/// This class extends the AAS meta model editing function for those related to
	/// SAMM (Semantic Aspect Meta Model) elements. 
	/// </summary>
	public class DispEditHelperSammModules : DispEditHelperModules
	{
		public SammIdSet SammExtensionHelperSelectSammVersion(IEnumerable<SammIdSet> idsets)
		{
			// create choices
			var fol = new List<AnyUiDialogueListItem>();
			foreach (var idset in idsets)
				fol.Add(new AnyUiDialogueListItem("" + idset.Version, idset));

			// prompt for this list
			var uc = new AnyUiDialogueDataSelectFromList(
				caption: "Select SAMM version to use ..");
			uc.ListOfItems = fol;
			this.context.StartFlyoverModal(uc);
			if (uc.Result && uc.ResultItem != null && uc.ResultItem.Tag != null &&
				uc.ResultItem.Tag.GetType().IsAssignableTo(typeof(Samm.SammIdSet)))
				return (SammIdSet)uc.ResultItem.Tag;
			return null;
		}

		public Type SammExtensionHelperSelectSammType(Type[] addableElements)
		{
			// create choices
			var fol = new List<AnyUiDialogueListItem>();
			foreach (var stp in addableElements)
				fol.Add(new AnyUiDialogueListItem("" + stp.Name, stp));

			// prompt for this list
			var uc = new AnyUiDialogueDataSelectFromList(
				caption: "Select SAMM element type to add ..");
			uc.ListOfItems = fol;
			this.context.StartFlyoverModal(uc);
			if (uc.Result && uc.ResultItem != null && uc.ResultItem.Tag != null &&
				((Type)uc.ResultItem.Tag).IsAssignableTo(typeof(Samm.ModelElement)))
				return (Type)uc.ResultItem.Tag;
			return null;
		}

		public static void SammExtensionHelperUpdateJson(Aas.IExtension se, Type sammType, Samm.ModelElement sammInst)
		{
			// trivial
			if (se == null || sammType == null || sammInst == null)
				return;

			// do a full fledged, carefull serialization
			string json = "";
			try
			{
				var settings = new JsonSerializerSettings
				{
					// SerializationBinder = new DisplayNameSerializationBinder(new[] { typeof(AasEventMsgEnvelope) }),
					NullValueHandling = NullValueHandling.Ignore,
					ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
					TypeNameHandling = TypeNameHandling.None,
					Formatting = Formatting.Indented
				};
				settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
				//settings.Converters.Add(new AdminShellConverters.AdaptiveAasIClassConverter(
				//	AdminShellConverters.AdaptiveAasIClassConverter.ConversionMode.AasCore));
				json = JsonConvert.SerializeObject(sammInst, sammType, settings);
			}
			catch (Exception ex)
			{
				LogInternally.That.SilentlyIgnoredError(ex);
			}

			// save this to the extension
			se.Value = json;
			se.ValueType = DataTypeDefXsd.String;
		}

		public AnyUiLambdaActionBase SammExtensionHelperSammReferenceAction<T>(
			Aas.Environment env,
			SammIdSet idSet,
			Aas.IReferable relatedReferable,
			int actionIndex,
			T sr,
			Action<T> setValue,
			Func<string, T> createInstance,
			string[] presetList = null,
			Type[] addableElements = null) where T : SammReference
		{
			if (actionIndex == 0 && presetList != null && presetList.Length > 0)
			{
				// prompt for this list
				var uc = new AnyUiDialogueDataSelectFromList(
					caption: "Select preset value to add ..");
				uc.ListOfItems = presetList.Select((st) => new AnyUiDialogueListItem("" + st, st)).ToList();
				this.context.StartFlyoverModal(uc);
				if (uc.Result && uc.ResultItem != null && uc.ResultItem.Tag != null &&
					uc.ResultItem.Tag is string prs)
				{
					setValue?.Invoke(createInstance?.Invoke("" + prs));
					return new AnyUiLambdaActionRedrawEntity();
				}
			}

			if (actionIndex == 1)
			{
				var k2 = SmartSelectAasEntityKeys(
					packages,
					PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
					"ConceptDescription");
				if (k2 != null && k2.Count >= 1)
				{
					setValue?.Invoke(createInstance?.Invoke("" + k2[0].Value));
					return new AnyUiLambdaActionRedrawEntity();
				}
			}

			if (actionIndex == 2)
			{
				// select type
				var sammTypeToCreate = SammExtensionHelperSelectSammType(
						addableElements: (addableElements != null)
							? addableElements : Samm.Constants.AddableElements);
				if (sammTypeToCreate == null)
					return new AnyUiLambdaActionNone();

				// select name
				var newUri = Samm.Util.ShortenUri(
					"" + (relatedReferable as Aas.IIdentifiable)?.Id);
				var uc = new AnyUiDialogueDataTextBox(
					"New Id for SAMM element:",
					symbol: AnyUiMessageBoxImage.Question,
					maxWidth: 1400,
					text: "" + newUri);
				if (!this.context.StartFlyoverModal(uc))
					return new AnyUiLambdaActionNone();
				newUri = uc.Text;

				// select idShort
				var newIdShort = Samm.Util.LastWordOfUri(newUri);
				var uc2 = new AnyUiDialogueDataTextBox(
					"New idShort for SAMM element:",
					symbol: AnyUiMessageBoxImage.Question,
					maxWidth: 1400,
					text: "" + newIdShort);
				if (!this.context.StartFlyoverModal(uc2))
					return new AnyUiLambdaActionNone();
				newIdShort = uc2.Text;
				if (newIdShort.HasContent() != true)
				{
					newIdShort = env?.ConceptDescriptions?
						.IterateIdShortTemplateToBeUnique("samm{0:0000}", 9999);
				}

				// make sure, the name is a new, valid Id for CDs
				if (newUri?.HasContent() != true ||
					null != env?.FindConceptDescriptionById(newUri))
				{
					Log.Singleton.Error("Invalid (used?) Id for a new ConceptDescriptin. Aborting!");
					return new AnyUiLambdaActionNone();
				}

				// add the new name to the current element
				setValue?.Invoke(createInstance?.Invoke(newUri));

				// now create a new CD for the new SAMM element
				var newCD = new Aas.ConceptDescription(
					id: newUri,
					idShort: newIdShort);

				// create new SAMM element 
				var newSamm = Activator.CreateInstance(
					sammTypeToCreate, new object[] { }) as Samm.ModelElement;

				var newSammSsd = newSamm as Samm.ISammSelfDescription;

				var newSammExt = new Aas.Extension(
						name: "" + newSammSsd?.GetSelfName(),
						semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
							(new[] {
								new Aas.Key(KeyTypes.GlobalReference,
								"" + idSet?.SelfNamespaces.ExtendUri(newSammSsd.GetSelfUrn(idSet.Version)))
							})
								.Cast<Aas.IKey>().ToList()),
						value: "");
				newCD.Extensions = new List<IExtension> { newSammExt };

				// fill with empty data content for SAMM
				SammExtensionHelperUpdateJson(newSammExt, sammTypeToCreate, newSamm);

				// save CD
				env?.ConceptDescriptions?.Add(newCD);

				// now, jump to this new CD
				return new AnyUiLambdaActionRedrawAllElements(nextFocus: newCD, isExpanded: true);
			}

			if (actionIndex == 3 && sr?.Value?.HasContent() == true)
			{
				return new AnyUiLambdaActionNavigateTo(
					new Aas.Reference(
							Aas.ReferenceTypes.ModelReference,
							new Aas.IKey[] {
										new Aas.Key(KeyTypes.ConceptDescription, sr.Value)
							}.ToList()));
			}

			return new AnyUiLambdaActionNone();
		}

		public void SammExtensionHelperAddSammReference<T>(
			Aas.Environment env, SammIdSet idSet,
			AnyUiStackPanel stack, string caption,
			Samm.ModelElement sammInst,
			Aas.IReferable relatedReferable,
			T sr,
			Action<T> setValue,
			Func<string, T> createInstance,
			int firstColumnWidth = -1, // -1 = Standard
			string[] presetList = null,
			bool showButtons = true,
			bool editOptionalFlag = false,
			Type[] addableElements = null) where T : SammReference
		{
			var grid = AddSmallGrid(1, 2, colWidths: new[] { "*", "#" });
			stack.Add(grid);
			var g1stack = AddSmallStackPanelTo(grid, 0, 0, margin: new AnyUiThickness(0));

			AddKeyValueExRef(
				g1stack, "" + caption, sammInst,
				value: "" + sr?.Value, null, repo,
				setValue: v =>
				{
					setValue?.Invoke(createInstance?.Invoke((string)v));
					return new AnyUiLambdaActionNone();
				},
				keyVertCenter: true,
				firstColumnWidth: firstColumnWidth,
				auxButtonTitles: !showButtons ? null : new[] { "Preset", "Existing", "New", "Jump" },
				auxButtonToolTips: !showButtons ? null : new[] {
					"Select from given presets.",
					"Select existing ConceptDescription.",
					"Create a new ConceptDescription for SAMM use.",
					"Jump to ConceptDescription with given Id."
				},
				auxButtonLambda: (i) =>
				{
					return SammExtensionHelperSammReferenceAction<T>(
						env, idSet, 
						relatedReferable,
						i,
						sr: sr,
						setValue: setValue,
						createInstance: createInstance,
						presetList: presetList,
						addableElements: addableElements);
				});

			if (editOptionalFlag && sr is OptionalSammReference osr)
			{
				AnyUiUIElement.RegisterControl(
					AddSmallCheckBoxTo(grid, 0, 1,
						margin: new AnyUiThickness(2, 2, 2, 2),
						verticalAlignment: AnyUiVerticalAlignment.Center,
						verticalContentAlignment: AnyUiVerticalAlignment.Center,
						content: "Opt.",
						isChecked: osr.Optional),
						(v) =>
						{
							osr.Optional = (bool)v;
							setValue?.Invoke(sr);
							return new AnyUiLambdaActionNone();
						});
			}
		}

		public void SammExtensionHelperAddListOfSammReference<T>(
			Aas.Environment env, SammIdSet idSet,
			AnyUiStackPanel stack, string caption,
			Samm.ModelElement sammInst,
			Aas.IReferable relatedReferable,
			List<T> value,
			Action<List<T>> setValue,
			Func<string, T> createInstance,
			bool editOptionalFlag,
			Type[] addableElements = null) where T : SammReference
		{
			this.AddVerticalSpace(stack);

			if (this.SafeguardAccess(stack, repo, value, "" + caption + ":",
				"Create data element!",
				v => {
					setValue?.Invoke(new List<T>(new T[] { createInstance?.Invoke("") }));
					return new AnyUiLambdaActionRedrawEntity();
				}))
			{
				// Head
				var sg = this.AddSubGrid(stack, "" + caption + ":",
				rows: 1 + value.Count, cols: 2,
				minWidthFirstCol: GetWidth(FirstColumnWidth.Standard),
				paddingCaption: new AnyUiThickness(5, 0, 0, 0),
				colWidths: new[] { "*", "#" });

				AnyUiUIElement.RegisterControl(
					AddSmallButtonTo(sg, 0, 1,
					margin: new AnyUiThickness(2, 2, 2, 2),
					padding: new AnyUiThickness(1, 0, 1, 0),
					content: "\u2795"),
					(v) =>
					{
						value.Add(createInstance?.Invoke(""));
						setValue?.Invoke(value);
						return new AnyUiLambdaActionRedrawEntity();
					});

				// individual references
				for (int lsri = 0; lsri < value.Count; lsri++)
				{
					// remember lambda safe
					var theLsri = lsri;

					// Stack in the 1st column
					var sp1 = AddSmallStackPanelTo(sg, 1 + lsri, 0);
					SammExtensionHelperAddSammReference(
						env, idSet,
						sp1, $"[{1 + lsri}]",
						(Samm.ModelElement)sammInst, relatedReferable,
						value[lsri],
						firstColumnWidth: 40,
						showButtons: false,
						editOptionalFlag: editOptionalFlag,
						addableElements: addableElements,
						setValue: (v) => {
							value[theLsri] = v;
							setValue?.Invoke(value);
						},
						createInstance: createInstance);

					if (false)
					{
						// remove button
						AnyUiUIElement.RegisterControl(
							AddSmallButtonTo(sg, 1 + lsri, 1,
							margin: new AnyUiThickness(2, 2, 2, 2),
							padding: new AnyUiThickness(5, 0, 5, 0),
							content: "-"),
							(v) =>
							{
								value.RemoveAt(theLsri);
								setValue?.Invoke(value);
								return new AnyUiLambdaActionRedrawEntity();
							});
					}
					else
					{
						// button [hamburger]
						AddSmallContextMenuItemTo(
							sg, 1 + lsri, 1,
							"\u22ee",
							repo, new[] {
								"\u2702", "Delete",
								"\u25b2", "Move Up",
								"\u25bc", "Move Down",
								"\U0001F4D1", "Select from preset",
								"\U0001F517", "Select from existing CDs",
								"\U0001f516", "Create new CD for SAMM",
								"\U0001f872", "Jump to"
							},
							margin: new AnyUiThickness(2, 2, 2, 2),
							padding: new AnyUiThickness(5, 0, 5, 0),
							menuItemLambda: (o) =>
							{
								var action = false;

								if (o is int ti)
									switch (ti)
									{
										case 0:
											value.RemoveAt(theLsri);
											action = true;
											break;
										case 1:
											MoveElementInListUpwards<T>(value, value[theLsri]);
											action = true;
											break;
										case 2:
											MoveElementInListDownwards<T>(value, value[theLsri]);
											action = true;
											break;
										case 3:
										case 4:
										case 5:
										case 6:
											return SammExtensionHelperSammReferenceAction<T>(
												env, idSet, relatedReferable,
												sr: value[theLsri],
												actionIndex: ti - 3,
												presetList: null,
												setValue: (srv) =>
												{
													value[theLsri] = srv;
													setValue?.Invoke(value);
												},
												createInstance: createInstance);
									}

								if (action)
								{
									setValue?.Invoke(value);
									return new AnyUiLambdaActionRedrawEntity();
								}
								return new AnyUiLambdaActionNone();
							});
					}
				}
			}

		}

		public void SammExtensionHelperAddCompleteModelElement(
			Aas.Environment env, SammIdSet idSet,
			AnyUiStackPanel stack,
			Samm.ModelElement sammInst,
			Aas.IReferable relatedReferable,
			Action<Samm.ModelElement> setValue)
		{
			// access
			if (env == null || stack == null || sammInst == null)
				return;

			// visually ease
			this.AddVerticalSpace(stack);

			// okay, try to build up a edit field by reflection
			var propInfo = sammInst.GetType().GetProperties();
			for (int pi = 0; pi < propInfo.Length; pi++)
			{
				// access
				var pii = propInfo[pi];

				// some type investigation
				var propType = pii.PropertyType;
				var underlyingType = Nullable.GetUnderlyingType(propType);

				// try to access flags
				var propFlags = "" + pii.GetCustomAttribute<Samm.SammPropertyFlagsAttribute>()?.Flags;
				var propFlagsLC = propFlags.ToLower();

				// List of SammReference?
				if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.SammReference>)))
				{
					var addableElements = Samm.Constants.AddableElements;
					if (propFlagsLC.Contains("contraints"))
						addableElements = Samm.Constants.AddableConstraints;

					SammExtensionHelperAddListOfSammReference<Samm.SammReference>(
						env, idSet, stack, caption: "" + pii.Name,
						(ModelElement)sammInst,
						relatedReferable,
						editOptionalFlag: false,
						value: (List<Samm.SammReference>)pii.GetValue(sammInst),
						setValue: (v) =>
						{
							pii.SetValue(sammInst, v);
							setValue?.Invoke(sammInst);
						},
						createInstance: (sr) => new SammReference(sr));
				}

				// List of optional SammReference?
				if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.OptionalSammReference>)))
				{
					SammExtensionHelperAddListOfSammReference<Samm.OptionalSammReference>(
						env, idSet, stack, caption: "" + pii.Name,
						(ModelElement)sammInst,
						relatedReferable,
						editOptionalFlag: true,
						value: (List<Samm.OptionalSammReference>)pii.GetValue(sammInst),
						setValue: (v) =>
						{
							pii.SetValue(sammInst, v);
							setValue?.Invoke(sammInst);
						},
						createInstance: (sr) => new OptionalSammReference(sr));
				}

				// List of LangString
				if (pii.PropertyType.IsAssignableTo(typeof(List<LangString>)))
				{
					// space
					this.AddVerticalSpace(stack);

					// get data
					var lls = (List<LangString>)pii.GetValue(sammInst);

					// handle null
					Action<List<Aas.ILangStringTextType>> lambdaSetValue = (v) =>
					{
						var back = v?.Select((ls) => new Samm.LangString(ls)).ToList();
						pii.SetValue(sammInst, back);
						setValue?.Invoke(sammInst);
					};

					if (this.SafeguardAccess(stack, repo, lls, "" + pii.Name + ":",
						"Create data element!",
						v =>
						{
							lambdaSetValue(new List<Aas.ILangStringTextType>());
							return new AnyUiLambdaActionRedrawEntity();
						}))
					{
						// get values
						var forth = lls?.Select(
								(ls) => (new Aas.LangStringTextType(ls.Language, ls.Text)) 
								as Aas.ILangStringTextType).ToList();

						// edit fields
						AddKeyListLangStr<Aas.ILangStringTextType>(
							stack, "" + pii.Name, forth, repo, relatedReferable,
							emitCustomEvent: (rf) =>
							{
								lambdaSetValue(forth);
								return new AnyUiLambdaActionNone();
							});
					}
				}

				// NamespaceMap
				if (pii.PropertyType.IsAssignableTo(typeof(Samm.NamespaceMap)))
				{
					this.AddVerticalSpace(stack);

					var lsr = (Samm.NamespaceMap)pii.GetValue(sammInst);

					Action<Samm.NamespaceMap> lambdaSetValue = (v) =>
					{
						pii.SetValue(sammInst, v);
						setValue?.Invoke(sammInst);
					};

					if (this.SafeguardAccess(stack, repo, lsr, "" + pii.Name + ":",
						"Create data element!",
						v =>
						{
							lambdaSetValue(new Samm.NamespaceMap());
							return new AnyUiLambdaActionRedrawEntity();
						}))
					{
						// Head
						var sg = this.AddSubGrid(stack, "" + pii.Name + ":",
									rows: 1 + lsr.Count(), cols: 3,
									minWidthFirstCol: GetWidth(FirstColumnWidth.Standard),
									paddingCaption: new AnyUiThickness(5, 0, 0, 0),
									colWidths: new[] { "80:", "*", "#" });

						AnyUiUIElement.RegisterControl(
							AddSmallButtonTo(sg, 0, 2,
							margin: new AnyUiThickness(2, 2, 2, 2),
							padding: new AnyUiThickness(1, 0, 1, 0),
							content: "\u2795"),
							(v) =>
							{
								lsr.AddOrIgnore(":", "");
								lambdaSetValue(lsr);
								return new AnyUiLambdaActionRedrawEntity();
							});

						// individual references
						for (int lsri = 0; lsri < lsr.Count(); lsri++)
						{
							var theLsri = lsri;

							// prefix										
							AnyUiUIElement.RegisterControl(
								AddSmallTextBoxTo(sg, 1 + theLsri, 0,
									text: lsr[theLsri].Prefix,
									margin: new AnyUiThickness(4, 2, 2, 2)),
									(v) =>
									{
										lsr[theLsri].Prefix = (string)v;
										pii.SetValue(sammInst, lsr);
										setValue?.Invoke(sammInst);
										return new AnyUiLambdaActionNone();
									});

							// uri										
							AnyUiUIElement.RegisterControl(
								AddSmallTextBoxTo(sg, 1 + theLsri, 1,
									text: lsr[theLsri].Uri,
									margin: new AnyUiThickness(2, 2, 2, 2)),
									(v) =>
									{
										lsr[theLsri].Uri = (string)v;
										pii.SetValue(sammInst, lsr);
										setValue?.Invoke(sammInst);
										return new AnyUiLambdaActionNone();
									});

							// minus
							AnyUiUIElement.RegisterControl(
								AddSmallButtonTo(sg, 1 + theLsri, 2,
								margin: new AnyUiThickness(2, 2, 2, 2),
								padding: new AnyUiThickness(5, 0, 5, 0),
								content: "-"),
								(v) =>
								{
									lsr.RemoveAt(theLsri);
									pii.SetValue(sammInst, lsr);
									setValue?.Invoke(sammInst);
									return new AnyUiLambdaActionRedrawEntity();
								});
						}
					}
				}

				// List of Constraint?
				if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.Constraint>)))
				{
					;
				}

				// single SammReference?
				if (pii.PropertyType.IsAssignableTo(typeof(Samm.SammReference)))
				{
					this.AddVerticalSpace(stack);

					var sr = (Samm.SammReference)pii.GetValue(sammInst);

					// preset attribute
					string[] presetValues = null;
					var x3 = pii.GetCustomAttribute<Samm.SammPresetListAttribute>();
					if (x3 != null)
					{
						presetValues = Samm.Constants.GetPresetsForListName(x3.PresetListName);
					}

					SammExtensionHelperAddSammReference<SammReference>(
						env, idSet, 
						stack, "" + pii.Name, (Samm.ModelElement)sammInst, relatedReferable,
						sr,
						presetList: presetValues,
						setValue: (v) => {
							pii.SetValue(sammInst, v);
							setValue?.Invoke(sammInst);
						},
						createInstance: (sr) => new SammReference(sr));
				}

				// List of string?
				if (pii.PropertyType.IsAssignableTo(typeof(List<string>)))
				{
					this.AddVerticalSpace(stack);

					var ls = (List<string>)pii.GetValue(sammInst);
					if (ls == null)
					{
						// Log.Singleton.Error("Internal error in SAMM element. Aborting.");
						continue;
					}

					var sg = this.AddSubGrid(stack, "" + pii.Name + ":",
						rows: 1 + ls.Count, cols: 2,
						minWidthFirstCol: GetWidth(FirstColumnWidth.Standard),
						paddingCaption: new AnyUiThickness(5, 0, 0, 0),
						colWidths: new[] { "*", "#" });

					AnyUiUIElement.RegisterControl(
						AddSmallButtonTo(sg, 0, 1,
							margin: new AnyUiThickness(2, 2, 2, 2),
							padding: new AnyUiThickness(5, 0, 5, 0),
							content: "Add blank"),
							(v) =>
							{
								ls.Add("");
								pii.SetValue(sammInst, ls);
								setValue?.Invoke(sammInst);
								return new AnyUiLambdaActionRedrawEntity();
							});

					for (int lsi = 0; lsi < ls.Count; lsi++)
					{
						var theLsi = lsi;
						var tb = AnyUiUIElement.RegisterControl(
							AddSmallTextBoxTo(sg, 1 + lsi, 0,
								text: ls[lsi],
								margin: new AnyUiThickness(2, 2, 2, 2)),
								(v) =>
								{
									ls[theLsi] = (string)v;
									pii.SetValue(sammInst, ls);
									setValue?.Invoke(sammInst);
									return new AnyUiLambdaActionRedrawEntity();
								});

						AnyUiUIElement.RegisterControl(
							AddSmallButtonTo(sg, 1 + lsi, 1,
								margin: new AnyUiThickness(2, 2, 2, 2),
								padding: new AnyUiThickness(5, 0, 5, 0),
								content: "-"),
								(v) =>
								{
									ls.RemoveAt(theLsi);
									pii.SetValue(sammInst, ls);
									setValue?.Invoke(sammInst);
									return new AnyUiLambdaActionRedrawEntity();
								});
					}
				}

				// single string?
				if (pii.PropertyType.IsAssignableTo(typeof(string)))
				{
					var isMultiLineAttr = pii.GetCustomAttribute<Samm.SammMultiLineAttribute>();

					Func<object, AnyUiLambdaActionBase> setValueLambda = (v) =>
					{
						pii.SetValue(sammInst, v);
						setValue?.Invoke(sammInst);
						return new AnyUiLambdaActionNone();
					};

					if (isMultiLineAttr == null)
					{
						// 1 line
						AddKeyValueExRef(
							stack, "" + pii.Name, sammInst, (string)pii.GetValue(sammInst), null, repo,
							setValue: setValueLambda);
					}
					else
					{
						// makes sense to have a bit vertical space
						AddVerticalSpace(stack);

						// multi line
						AddKeyValueExRef(
							stack, "" + pii.Name, sammInst, (string)pii.GetValue(sammInst), null, repo,
							setValue: setValueLambda,
							limitToOneRowForNoEdit: true,
							maxLines: isMultiLineAttr.MaxLines.Value,
							auxButtonTitles: new[] { "\u2261" },
							auxButtonToolTips: new[] { "Edit in multiline editor" },
							auxButtonLambda: (buttonNdx) =>
							{
								if (buttonNdx == 0)
								{
									var uc = new AnyUiDialogueDataTextEditor(
										caption: $"Edit " + pii.Name,
										mimeType: System.Net.Mime.MediaTypeNames.Text.Plain,
										text: (string)pii.GetValue(sammInst));
									if (this.context.StartFlyoverModal(uc))
									{
										pii.SetValue(sammInst, uc.Text);
										setValue?.Invoke(sammInst);
										return new AnyUiLambdaActionRedrawEntity();
									}
								}
								return new AnyUiLambdaActionNone();
							});
					}
				}

				// single uint?
				if (pii.PropertyType.IsAssignableTo(typeof(uint?)))
				{
					Func<object, AnyUiLambdaActionBase> setValueLambda = (v) =>
					{
						if (v == null || ((string)v).Trim().Length < 1)
							pii.SetValue(sammInst, null);
						else
							if (uint.TryParse((string)v, out var result))
							pii.SetValue(sammInst, result);
						setValue?.Invoke(sammInst);
						return new AnyUiLambdaActionNone();
					};

					var input = (uint?)pii.GetValue(sammInst);
					string value = "";
					if (input.HasValue)
						value = input.Value.ToString();

					// 1 line
					AddKeyValueExRef(
						stack, "" + pii.Name, sammInst,
						value,
						null, repo,
						setValue: setValueLambda,
						maxLines: 1);
				}

				// nullable enum?
				if (underlyingType != null && underlyingType.IsEnum)
				{
					// a little space
					AddVerticalSpace(stack);

					// current enum member
					var currEM = pii.GetValue(sammInst);

					// generate a list for combo box
					var eMems = EnumHelper.EnumHelperGetMemberInfo(underlyingType).ToList();

					// find selected index
					int? selectedIndex = null;
					for (int emi = 0; emi < eMems.Count; emi++)
					{
						if (((int)eMems[emi].MemberInstance) == ((int)currEM))
							selectedIndex = emi;
					}

					// add a container
					var sg = this.AddSubGrid(stack, "" + pii.Name + ":",
						rows: 1, cols: 2,
						minWidthFirstCol: GetWidth(FirstColumnWidth.Standard),
						paddingCaption: new AnyUiThickness(5, 0, 0, 0),
						marginGrid: new AnyUiThickness(4, 0, 0, 0),
						colWidths: new[] { "*", "#" });

					// and combobox inside
					AnyUiComboBox cb = null;
					cb = AnyUiUIElement.RegisterControl(
						AddSmallComboBoxTo(
							sg, 0, 0,
							minWidth: 120,
							margin: NormalOrCapa(
								new AnyUiThickness(4, 1, 2, 3),
								AnyUiContextCapability.Blazor, new AnyUiThickness(4, 2, 2, 0)),
							padding: NormalOrCapa(
								new AnyUiThickness(2, 1, 2, 1),
								AnyUiContextCapability.Blazor, new AnyUiThickness(0, 4, 0, 4)),
							selectedIndex: selectedIndex,
							items: eMems.Select((mi) => mi.MemberValue).ToArray()),
						setValue: (o) =>
						{
							if (cb.SelectedIndex.HasValue
								&& cb.SelectedIndex.Value >= 0
								&& cb.SelectedIndex.Value < eMems.Count)
							{
								pii.SetValue(sammInst, eMems[cb.SelectedIndex.Value].MemberInstance);
								setValue?.Invoke(sammInst);
							}
							return new AnyUiLambdaActionNone();
						});
				}
			}
		}

		/// <summary>
		/// Shall provide rather quick access to information ..
		/// </summary>
		public static Type CheckReferableForSammExtensionType(Aas.IReferable rf)
		{
			// access
			if (rf?.Extensions == null)
				return null;

			// find any?
			foreach (var se in rf.Extensions)
			{
				var sammIdSetType = Samm.SammIdSets.GetAnyIdSetTypeFromUrn(Samm.Util.GetSammUrn(se));
				if (sammIdSetType != null)
					return sammIdSetType.Item2;
			}

			// no?
			return null;
		}

		public static IEnumerable<ModelElement> CheckReferableForSammElements(Aas.IReferable rf)
		{
			// access
			if (rf?.Extensions == null)
				yield break;

			// find any?
			foreach (var se in rf.Extensions)
			{
				// get type 
				var sammIdSetType = Samm.SammIdSets.GetAnyIdSetTypeFromUrn(Samm.Util.GetSammUrn(se));
				if (sammIdSetType == null)
					continue;

				// get instance data
				ModelElement sammInst = null;

				// try to de-serializa extension value
				try
				{
					if (se.Value != null)
						sammInst = JsonConvert.DeserializeObject(se.Value, sammIdSetType.Item2) as ModelElement;
				}
				catch (Exception ex)
				{
					LogInternally.That.SilentlyIgnoredError(ex);
					sammInst = null;
				}

				if (sammInst == null)
					continue;

				// give back
				yield return sammInst;
			}
		}

		/// <summary>
		/// Shall provide rather quick access to information ..
		/// </summary>
		/// <returns>Null, if not a SAMM model element</returns>
		public static string CheckReferableForSammExtensionTypeName(Type sammType)
		{
			return Samm.SammIdSets.GetAnyNameFromSammType(sammType);
		}

		public void DisplayOrEditEntitySammExtensions(
			Aas.Environment env, 
			AnyUiStackPanel stack,
			List<Aas.IExtension> sammExtension,
			Action<List<Aas.IExtension>> setOutput,
			string[] addPresetNames = null, List<Aas.IKey>[] addPresetKeyLists = null,
			Aas.IReferable relatedReferable = null,
			AasxMenu superMenu = null)
		{
			// access
			if (stack == null)
				return;

			// members
			this.AddGroup(stack, "SAMM extensions \u00ab experimental \u00bb :", levelColors.MainSection);

			this.AddHintBubble(
				stack, hintMode,
				new[] {
					new HintCheck(
						() => { return sammExtension == null ||
							sammExtension.Count < 1; },
						"Eclipse Semantic Aspect Meta Model (SAMM) allows the creation of models to describe " +
						"the semantics of digital twins by defining their domain specific aspects. " +
						"This version of the AASX Package Explorer allows expressing Characteristics of SAMM " +
						"as an extension of ConceptDescriptions. In later versions, this is assumed to be " +
						"realized by DataSpecifications.",
						breakIfTrue: true, severityLevel: HintCheck.Severity.Notice),
					new HintCheck(
						() => { return sammExtension.Where(p => Samm.Util.HasSammSemanticId(p)).Count() > 1; },
						"Only one SAMM extension is allowed per concept.",
						breakIfTrue: true),
				});
			if (this.SafeguardAccess(
					stack, this.repo, sammExtension, "SAMM extensions:", "Create data element!",
					v =>
					{
						setOutput?.Invoke(new List<Aas.IExtension>());
						return new AnyUiLambdaActionRedrawEntity();
					}))
			{
				// Note: the buttons will use a "detected version" (see below)
				// of the id set
				SammIdSet detectedIdSet = null;

				// head control
				if (editMode)
				{
					// let the user control the number of references
					this.AddActionPanel(
						stack, "Spec. records:", repo: repo,
						superMenu: superMenu,
						ticketMenu: new AasxMenu()
							.AddAction("add-aspect", "Add Aspect",
								"Add single top level of any SAMM aspect model.")
							.AddAction("add-property", "Add Property",
								"Add a named value element to the aspect or its sub-entities.")
							.AddAction("add-characteristic", "Add Characteristic",
								"Characteristics describe abstract concepts that must be made specific when they are used.")
							.AddAction("auto-entity", "Add Entity",
								"An entity is the main element to collect a set of properties.")
							.AddAction("auto-other", "Add other ..",
								"Adds an other Characteristic by selecting from a list.")
							.AddAction("delete-last", "Delete last extension",
								"Deletes last extension."),
						ticketAction: (buttonNdx, ticket) =>
						{
							Samm.ModelElement newChar = null;
							switch (buttonNdx)
							{
								case 0:
									newChar = new Samm.Aspect();
									break;
								case 1:
									newChar = new Samm.Property();
									break;
								case 2:
									newChar = new Samm.Characteristic();
									break;
								case 3:
									newChar = new Samm.Entity();
									break;
							}

							if (buttonNdx == 4)
							{							
								// select
								var sammTypeToCreate = SammExtensionHelperSelectSammType(
									Samm.Constants.AddableElements);

								if (sammTypeToCreate != null)
								{
									// to which?
									newChar = Activator.CreateInstance(
										sammTypeToCreate, new object[] { }) as Samm.ModelElement;
								}
							}

							// create a new element
							if (newChar != null && newChar is Samm.ISammSelfDescription ssd)
							{
								// which id set to use
								if (detectedIdSet == null)
									detectedIdSet = SammExtensionHelperSelectSammVersion(
										Samm.SammIdSets.IdSets.Values);
								if (detectedIdSet == null)
									return new AnyUiLambdaActionNone();

								// now add
								sammExtension.Add(
									new Aas.Extension(
										name: ssd.GetSelfName(),
										semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
											(new[] {
												new Aas.Key(KeyTypes.GlobalReference,
												"" + detectedIdSet.SelfNamespaces.ExtendUri(
														ssd.GetSelfUrn(detectedIdSet.Version)))
											})
											.Cast<Aas.IKey>().ToList()),
										value: ""));
							}
							
							// remove
							if (buttonNdx == 5)
							{
								if (sammExtension.Count > 0)
									sammExtension.RemoveAt(sammExtension.Count - 1);
								else
									setOutput?.Invoke(null);
							}

							this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
							return new AnyUiLambdaActionRedrawEntity();
						});
				}

				// now use the normal mechanism to deal with editMode or not ..
				if (sammExtension != null && sammExtension.Count > 0)
				{
					var numSammExt = 0;

					for (int i = 0; i < sammExtension.Count; i++)
					{
						// get type 
						var se = sammExtension[i];
						var idSetType = Samm.SammIdSets.GetAnyIdSetTypeFromUrn(Samm.Util.GetSammUrn(se));
						if (idSetType?.Item1 == null || idSetType.Item2 == null)
							continue;
						var sammType = idSetType.Item2;
						var idSet = idSetType.Item1;

						// remeber as detected .. (for later dialogs described above!)
						if (detectedIdSet == null)
							detectedIdSet = idSet;

						// more then one?
						this.AddHintBubble(
							stack, hintMode,
							new[] {
							new HintCheck(
								() => numSammExt > 0,
								"Only one SAMM extension per ConceptDescription allowed!",
								breakIfTrue: true)});

						// indicate
						numSammExt++;

						AnyUiFrameworkElement iconElem = null;
						var ri = Samm.Constants.GetRenderInfo(sammType);
						if (ri != null)
						{
							iconElem = new AnyUiBorder()
							{
								Background = new AnyUiBrush(ri.Background),
								BorderBrush = new AnyUiBrush(ri.Foreground),
								BorderThickness = new AnyUiThickness(2.0f),
								MinHeight = 50,
								MinWidth = 50,
								Child = new AnyUiTextBlock()
								{
									Text = "" + ri.Abbreviation,
									HorizontalAlignment = AnyUiHorizontalAlignment.Center,
									VerticalAlignment = AnyUiVerticalAlignment.Center,
									Foreground = new AnyUiBrush(ri.Foreground),
									Background = AnyUi.AnyUiBrushes.Transparent,
									FontSize = 2.0,
									FontWeight = AnyUiFontWeight.Bold
								},
								HorizontalAlignment = AnyUiHorizontalAlignment.Center,
								VerticalAlignment = AnyUiVerticalAlignment.Center,
								Margin = new AnyUiThickness(5, 0, 10, 0),
								SkipForTarget = AnyUiTargetPlatform.Browser
							};
						}

						this.AddGroup(stack, $"SAMM extension [{i + 1}]: {sammType.Name}",
							levelColors.SubSection.Bg, levelColors.SubSection.Fg,
							iconElement: iconElem);

						// get instance data
						Samm.ModelElement sammInst = null;
						if (false)
						{
							// Note: right now, create fresh instance
							sammInst = Activator.CreateInstance(sammType, new object[] { }) as Samm.ModelElement;
							if (sammInst == null)
							{
								stack.Add(new AnyUiLabel() { Content = "(unable to create instance data)" });
								continue;
							}
						}
						else
						{
							// try to de-serializa extension value
							try
							{
								if (se.Value != null)
									sammInst = JsonConvert.DeserializeObject(se.Value, sammType) as Samm.ModelElement;
							}
							catch (Exception ex)
							{
								LogInternally.That.SilentlyIgnoredError(ex);
								sammInst = null;
							}

							if (sammInst == null)
							{
								sammInst = Activator.CreateInstance(sammType, new object[] { }) as Samm.ModelElement;
							}
						}

						SammExtensionHelperAddCompleteModelElement(
							env, idSet, stack,
							sammInst: sammInst,
							relatedReferable: relatedReferable,
							setValue: (si) =>
							{
								SammExtensionHelperUpdateJson(se, si.GetType(), si);
							});

					}
				}
			}
		}		
		
	}

	/// <summary>
	/// This class adds some helpers for handling enums.
	/// </summary>
	public static class EnumHelper
	{
		// TODO (MIHO, 2023-11-03): move to general utility class
		public class EnumHelperMemberInfo
		{
			public string MemberValue = "";
			public string MemberDisplay = "";
			public object MemberInstance;
		}

		public static IEnumerable<EnumHelperMemberInfo> EnumHelperGetMemberInfo(Type underlyingType)
		{
			foreach (var enumMemberInfo in underlyingType.GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				var enumInst = Activator.CreateInstance(underlyingType);

				var memVal = enumMemberInfo.GetCustomAttribute<EnumMemberAttribute>()?.Value;
				var memDisp = enumMemberInfo.GetCustomAttribute<EnumMemberDisplayAttribute>()?.Text;				

				if (memVal?.HasContent() == true)
				{
					var ev = enumMemberInfo.GetValue(enumInst);

					yield return new EnumHelperMemberInfo()
					{
						MemberValue = memVal,
						MemberDisplay = (memDisp?.HasContent() == true) ? memDisp : memVal,
						MemberInstance = ev
					};
				}
			}
		}

		public static T GetEnumMemberFromValueString<T>(string valStr, T valElse = default(T) ) where T : struct
		{
			foreach (var em in EnumHelperGetMemberInfo(typeof(T)))
				if (em.MemberValue.Equals(valStr?.Trim(), StringComparison.InvariantCultureIgnoreCase))
					return (T) em.MemberInstance;
			return (T) valElse;
		}
	}

	/// <summary>
	/// This class provides a little help when dealing with RDF graphs provided by dotNetRdf
	/// </summary>
	public static class RdfHelper
	{
		public static bool IsTerminalNode(INode node)
		{
			if (node == null)
				return false;
			return node.NodeType == NodeType.Uri
				|| node.NodeType == NodeType.Literal;
		}

		public static string GetTerminalStrValue(INode node)
		{
			if (node == null)
				return "";
			if (node is LiteralNode ln)
				return ln.Value;
			return node.ToSafeString();
		}

		public static INode SafeCreateLiteralNode(IGraph g, string text, string language)
		{
			// access
			if (g == null || text == null)
				return null;

			// filter langauge
			var l2 = "";
			foreach (var c in language)
				if (c.IsLetter())
					l2 += c;

			// emergency?
			if (l2.Length < 2)
				l2 = "en";

			return g.CreateLiteralNode(text, l2);
		}

		public static INode CreateUriOrLiteralNode(
			IGraph g, SammIdSet idSet,
			string text, bool isUri)
		{
			// access
			if (g == null || idSet == null || text == null)
				return null;

			if (isUri)
				return g.CreateUriNode(new Uri(text, UriKind.RelativeOrAbsolute));
			else
				return g.CreateLiteralNode(text, datatype: new Uri(idSet.XsdString));
		}

		public static Aas.LangStringTextType ParseLangStringFromNode(INode node)
		{
			if (node is LiteralNode tpoln)
			{
				// use directly
				return new Aas.LangStringTextType(tpoln.Language, tpoln.Value);
			}
			else
			{
				// try to recover somehow
				var objStr = node.ToSafeString();
				var m = Regex.Match(objStr, @"(.*?)@([A-Za-z_-]+)");
				return (!m.Success)
					? new Aas.LangStringTextType("en?", "" + objStr)
					: new Aas.LangStringTextType(m.Groups[2].ToSafeString(), m.Groups[1].ToSafeString());
			}
		}
	}

	/// <summary>
	/// This class realizes SAMM import/ export to the AAS models.
	/// Note: instances of this class have internal state; import/ export 
	/// is expected to work only ONCE per lifetime of the instance!!!
	/// </summary>
	public class SammImportExport
	{
		/// <summary>
		/// Set to true by some import/ export function, if important messages occured
		/// </summary>
		public bool AnyInfoMessages = false;

		/// <summary>
		/// When auto generating nodes, this head will be used for IdShort.
		/// </summary>
		public string AutoFillHeadIdShort = "SAMM_";

		/// <summary>
		/// When auto generating nodes, this head will be used for Id.
		/// Note: this string will be changed, when the Aspect element is visited
		/// and prefixes are known better.
		/// </summary>
		public string AutoFillHeadId = "<TO BE FILLED>";

		/// <summary>
		/// Parses an rdf:Collection and reads out either <c>SammReference</c> or <c>OptionalSammReference</c>
		/// </summary>
		public List<T> ImportRdfCollection<T>(
			IGraph g, SammIdSet idSet,
			INode collectionStart,
			string contentRelationshipUri,
			Func<string, bool, T> createInstance) where T : SammReference
		{
			// Try parse a rdf:Collection
			// see: https://ontola.io/blog/ordered-data-in-rdf

			// access
			if (g == null || idSet == null)
				return null;

			// start
			var lsr = new List<T>();
			INode collPtr = collectionStart;

			if (collPtr != null && (collPtr.NodeType == NodeType.Uri || collPtr.NodeType == NodeType.Literal))
			{
				// only a single member is given
				var litVal = RdfHelper.GetTerminalStrValue(collPtr);
				if (litVal?.HasContent() == true
					&& litVal != idSet.RdfCollNil)
					lsr.Add(createInstance?.Invoke(litVal, false));
			}
			else
			{
				// a chain of instances is given
				while (collPtr != null && collPtr.NodeType == NodeType.Blank)
				{
					// the collection pointer needs to have a first relationship
					var firstRel = g.GetTriplesWithSubjectPredicate(
						subj: collPtr,
						pred: new UriNode(new Uri(idSet.RdfCollFirst)))
									.FirstOrDefault();
					if (firstRel?.Object == null)
						break;

					// investigate, if first.object is a automatic/composite or an end node
					if (firstRel.Object.NodeType == NodeType.Uri
						|| firstRel.Object.NodeType == NodeType.Literal)
					{
						// first.object is something tangible
						lsr.Add(createInstance?.Invoke(firstRel.Object.ToSafeString(), false));
					}
					else
					if (contentRelationshipUri?.HasContent() == true)
					{
						// crawl firstRel.Object further to get individual end notes
						string propElem = null;
						bool? optional = null;

						foreach (var x3 in g.GetTriplesWithSubject(firstRel.Object))
						{
							if (x3.Predicate.Equals(new UriNode(
									new Uri(contentRelationshipUri))))
								propElem = x3.Object.ToSafeString();
							if (x3.Predicate.Equals(
									new UriNode(new Uri(idSet.RdfCollOptional))))
								optional = x3.Object.ToSafeString() ==
									"true^^http://www.w3.org/2001/XMLSchema#boolean";
						}

						if (propElem != null)
							lsr.Add(createInstance?.Invoke(propElem, optional.Value));
					}

					// iterate further
					var restRel = g.GetTriplesWithSubjectPredicate(
						subj: collPtr,
						pred: new UriNode(new Uri(idSet.RdfCollRest)))
								.FirstOrDefault();
					collPtr = restRel?.Object;
				}
			}

			return lsr;
		}

		/// <summary>
		/// Small tuple for <c>ImportSammHelperCreateSubjectAndFill</c>
		/// </summary>
		public class ImportSammInfo
		{
			/// <summary>
			/// Type of samm model element determined by type node information.
			/// </summary>
			public Type SammType;

			/// <summary>
			/// Created samm model element with filled instance data
			/// </summary>
			public ModelElement SammInst;

			/// <summary>
			/// Id after Node and CD was written
			/// </summary>
			public string NewId;

			/// <summary>
			/// IdShort after Node and CD was written
			/// </summary>
			public string NewIdShort;

			/// <summary>
			/// Checks, if <c>SammType</c> and <c>SammInst</c> are valid.
			/// </summary>
			public bool IsValidInst() => SammType != null && SammInst != null;
		}

		/// <summary>
		/// Checks if <c>subjectNode</c> is a node, which can be parsed to a
		/// single SammReference (not a collection of nodes!)
		/// </summary>
		public SammReference ImportParseSingleSammReference(
			Aas.IEnvironment env,
			IGraph g, SammIdSet idSet,
			INode subjectNode)
		{
			// access
			if (g == null || idSet == null)
				return null;

			// anonymous node or note
			if (RdfHelper.IsTerminalNode(subjectNode))
			{
				// simply set the value
				return new SammReference(RdfHelper.GetTerminalStrValue(subjectNode));
			}
			else
			{
				// in order to be valid anonymous node, there needs to
				// the "a" relationship behind it
				var trpA = g.GetTriplesWithSubjectPredicate(
					subj: subjectNode,
					pred: new UriNode(new Uri(idSet.PredicateA)))?.FirstOrDefault();

				if (trpA == null)
					return null;

				// create an samm instance
				var sammInfo = ImportCreateSubjectAndFill(
					env, g, idSet, trpA.Subject, trpA.Object);

				if (sammInfo?.IsValidInst() != true)
					return null;

				// create CD for this
				ImportCreateCDandIds(
					env, idSet, sammInfo, autoFillIdShortAndId: true);
				// set this as reference ..
				return new SammReference(sammInfo.NewId);
			}
		}

		public ImportSammInfo ImportCreateSubjectAndFill(
			Aas.IEnvironment env,
			IGraph g,
			SammIdSet idSet,
			INode subjectNode,
			INode typeObjNode)
		{
			// access
			if (subjectNode == null || typeObjNode == null)
				return null;

			// check, if there is a SAMM type behind the object
			var sammElemUri = RdfHelper.GetTerminalStrValue(typeObjNode);
			var sammType = idSet.GetTypeFromUrn(sammElemUri);
			if (sammType == null)
			{
				Log.Singleton.Info($"Potential SAMM element found but unknown URI={sammElemUri}");
				return null;
			}

			// okay, create an instance
			var sammInst = Activator.CreateInstance(sammType, new object[] { }) as Samm.ModelElement;
			if (sammInst == null)
			{
				Log.Singleton.Error($"Error creating instance for SAMM element URI={sammElemUri}");
				return null;
			}

			// okay, try to find elements driven by the properties in the class
			// by reflection
			var propInfo = sammInst.GetType().GetProperties();
			for (int pi = 0; pi < propInfo.Length; pi++)
			{
				// reflection
				var pii = propInfo[pi];

				var propType = pii.PropertyType;
				var underlyingType = Nullable.GetUnderlyingType(propType);

				// need to have a custom attribute to identify the subject uri of the turtle triples
				var propSearchUri = Samm.Util.FindAnySammPropertyUriAttribute(pii, idSet.Version)?.Uri;
				if (propSearchUri == null)
					continue;

				// extend this
				propSearchUri = idSet.SelfNamespaces.ExtendUri(propSearchUri);

				//// now try to find triples with:
				//// Subject = trpSammElem.Subject and
				//// Predicate = propSearchUri
				foreach (var trpProp in g.GetTriplesWithSubjectPredicate(
					subj: subjectNode,
					pred: new VDS.RDF.UriNode(new Uri(propSearchUri))))
				{
					// now let the property type decide, how to 
					// put data into the property

					var objStr = RdfHelper.GetTerminalStrValue(trpProp.Object);
					var isTerminalNode = RdfHelper.IsTerminalNode(trpProp.Object);

					// List of Samm.LangString
					if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.LangString>)))
					{
						// multiple triples; each will go into one LangStr
						var ls = RdfHelper.ParseLangStringFromNode(trpProp.Object);

						// now, access the property
						var lls = (List<Samm.LangString>)pii.GetValue(sammInst);
						if (lls == null)
							lls = new List<Samm.LangString>();
						if (ls != null)
							lls.Add(new LangString(ls));
						pii.SetValue(sammInst, lls);
					}

					// List of string
					if (pii.PropertyType.IsAssignableTo(typeof(List<string>)))
					{
						// now, access the property
						var lls = (List<string>)pii.GetValue(sammInst);
						if (lls == null)
							lls = new List<string>();
						lls.Add(objStr);
						pii.SetValue(sammInst, lls);
					}

					// List of SammReference
					if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.SammReference>)))
					{
						// get the data
						var lsr = (List<Samm.SammReference>)pii.GetValue(sammInst);
						if (lsr == null)
							lsr = new List<Samm.SammReference>();

						// need a special uri for each content element
						// but not fully mandatory
						var collContentUri = Samm.Util.FindAnySammCollectionContentUriAttribute(
								pii, idSet.Version)?.Uri;

						// there are two possibilities, by spec/ knowledge not
						// easy to distinguish: an optional _single_ SammReference, which 
						// should be added to the list or a _list_ of SammReferences.
						// Approach: be open for the first, if not, check the second.
						var sr = ImportParseSingleSammReference(env, g, idSet, trpProp.Object);
						if (sr != null)
							lsr.Add(sr);
						else
							lsr.AddRange(
								ImportRdfCollection(
									g, idSet, collectionStart: trpProp.Object,
									contentRelationshipUri: 
										idSet.SelfNamespaces.ExtendUri(collContentUri), 
									createInstance: (sr, opt) => new SammReference(sr)));

						// write found references back
						pii.SetValue(sammInst, lsr);
					}

					// List of OptionalSammReference
					if (pii.PropertyType.IsAssignableTo(typeof(List<Samm.OptionalSammReference>)))
					{
						// get the data
						var lsr = (List<Samm.OptionalSammReference>)pii.GetValue(sammInst);
						if (lsr == null)
							lsr = new List<Samm.OptionalSammReference>();

						// need a special uri for each content element
						var collContentUri = Samm.Util.FindAnySammCollectionContentUriAttribute(
								pii, idSet.Version)?.Uri;

						// put it
						lsr.AddRange(
							ImportRdfCollection(
								g, idSet, collectionStart: trpProp.Object,
								contentRelationshipUri: 
									idSet.SelfNamespaces.ExtendUri(collContentUri),
								createInstance: (sr, opt) => new OptionalSammReference(sr, opt)));

						// write found references back
						pii.SetValue(sammInst, lsr);
					}

					// just SammReference
					if (pii.PropertyType.IsAssignableTo(typeof(Samm.SammReference)))
					{
						var sr = ImportParseSingleSammReference(env, g, idSet, trpProp.Object);
						if (sr != null)
							pii.SetValue(sammInst, sr);
					}

					// just string
					if (pii.PropertyType.IsAssignableTo(typeof(string)))
					{
						// simply set the value
						pii.SetValue(sammInst, objStr);
					}

					// just int?
					if (pii.PropertyType.IsAssignableTo(typeof(uint?)))
					{
						// simply set the value
						if (uint.TryParse(objStr, out var result))
						{
							uint? value = result;
							pii.SetValue(sammInst, value);
						}
					}

					// first check: any kind of enum?
					if (underlyingType != null && underlyingType.IsEnum)
					{

						var eMems = EnumHelper.EnumHelperGetMemberInfo(underlyingType);
						foreach (var em in eMems)
							if (objStr.Contains(em.MemberValue))
							{
								// set the enum type, even if it is a Nullable enum type ..
								pii.SetValue(sammInst, em.MemberInstance);
							}
					}
				}
			}

			// okay
			return new ImportSammInfo()
			{
				SammType = sammType,
				SammInst = sammInst
			};
		}

		public void ImportCreateCDandIds(
			Aas.IEnvironment env,
			SammIdSet idSet,
			ImportSammInfo si,
			string givenUriId = null,
			string overIdShort = null,
			bool autoFillIdShortAndId = false,
			List<ILangStringTextType> cdDesc = null)
		{
			// access
			if (si?.IsValidInst() != true || idSet == null)
				return;

			// which identifiers?
			if (autoFillIdShortAndId)
			{
				// first generate a idShort
				si.NewIdShort = env?.ConceptDescriptions?
					.IterateIdShortTemplateToBeUnique(AutoFillHeadIdShort + "auto{0:0000}", 9999);

				// make this as id also
				si.NewId = "" + AutoFillHeadId + si.NewIdShort;

				// info user
				Log.Singleton.Info($"SAMM import: auto-generating element Id/ IdShort {si.NewId} ..");
				AnyInfoMessages = true;
			}
			else
			{
				if (givenUriId?.HasContent() != true)
					return;
				si.NewId = givenUriId;
				si.NewIdShort = Samm.Util.LastWordOfUri(si.NewId);
				if (overIdShort?.HasContent() == true)
					si.NewIdShort = overIdShort;
				if (si.NewIdShort.HasContent() != true)
				{
					si.NewIdShort = env?.ConceptDescriptions?
						.IterateIdShortTemplateToBeUnique(AutoFillHeadIdShort + "auto{0:0000}", 9999);
				}
			}

			// now create a new CD for the new SAMM element
			var newCD = new Aas.ConceptDescription(
				id: si.NewId,
				idShort: si.NewIdShort,
				description: cdDesc);

			// create new SAMM element 
			var newSammSsd = si.SammInst as Samm.ISammSelfDescription;
			var newSammExt = new Aas.Extension(
					name: "" + newSammSsd?.GetSelfName(),
					semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
						(new[] {
							new Aas.Key(KeyTypes.GlobalReference,
							"" + idSet.SelfNamespaces.ExtendUri(newSammSsd.GetSelfUrn(idSet.Version)))
						})
						.Cast<Aas.IKey>().ToList()),
					value: "");
			newCD.Extensions = new List<IExtension> { newSammExt };

			// fill with empty data content for SAMM
			DispEditHelperSammModules.SammExtensionHelperUpdateJson(newSammExt, si.SammType, si.SammInst);

			// save CD
			env?.ConceptDescriptions?.Add(newCD);
		}		

		public void ImportSammModelToConceptDescriptions(
			Aas.IEnvironment env, 
			string fn)
		{
			// do it
			IGraph g = new Graph();
			TurtleParser ttlparser = new TurtleParser();

			// Load text to find header comments
			Log.Singleton.Info($"Reading SAMM file for text cmments: {fn} ..");
			var globalComments = string.Join(System.Environment.NewLine,
					System.IO.File.ReadAllLines(fn)
						.Where((ln) => ln.Trim().StartsWith('#')));

			// Load graph using a Filename
			Log.Singleton.Info($"Reading SAMM file for tutle graph: {fn} ..");
			ttlparser.Load(g, fn);

			// Load namespace map
			var globalNamespaces = new Samm.NamespaceMap();
			if (g.NamespaceMap != null)
				foreach (var pf in g.NamespaceMap.Prefixes)
				{
					var prefix = pf.Trim();
					if (!prefix.EndsWith(':'))
						prefix += ":";
					globalNamespaces.AddOrIgnore(prefix, g.NamespaceMap.GetNamespaceUri(pf).ToSafeString());
				}

			// figure out, which idSet to be used
			// var idSet = Samm.SammIdSets.IdSets.Values.Last();
			var idSet = Samm.SammIdSets.DetectVersion(globalNamespaces);
			if (idSet == null)
			{
				Log.Singleton.Error("Cannot determine SAMM version for the model file. Aborting!");
				return;
			}

			// find all potential SAMM elements " :xxx a bamm:XXXX"
			foreach (var trpSammElem in g.GetTriplesWithPredicate(new Uri(idSet.PredicateA)))
			{
				// it only make sense, that the subject of the found triples is a
				// UriNode. A anonymous node would NOT make sense, here
				if (trpSammElem.Subject.NodeType != NodeType.Uri)
					continue;

				// very soon check if to use an Aspect namespace

				// create the samm element 
				var sammInfo = ImportCreateSubjectAndFill(
					env, g, idSet, trpSammElem.Subject, trpSammElem.Object);

				if (sammInfo?.IsValidInst() != true)
					continue;

				// description of Referable is a special case
				List<Aas.LangStringTextType> cdDesc = null;
				var descPred = idSet.SelfNamespaces.ExtendUri(idSet.SammDescription);
				foreach (var trpProp in g.GetTriplesWithSubjectPredicate(
						subj: trpSammElem.Subject,
						pred: new VDS.RDF.UriNode(new Uri(descPred))))
				{
					// decompose
					var ls = RdfHelper.ParseLangStringFromNode(trpProp.Object);
				
					// add
					if (cdDesc == null)
						cdDesc = new List<LangStringTextType>();
					if (ls != null)
						cdDesc.Add(ls);
				}

				// name of elements is a special case. Can become idShort
				string elemName = null;
				var elemPred = idSet.SelfNamespaces.ExtendUri(idSet.SammName);
				foreach (var trpProp in g.GetTriplesWithSubjectPredicate(
						subj: trpSammElem.Subject,
						pred: new VDS.RDF.UriNode(new Uri(elemPred))))
				{
					elemName = RdfHelper.GetTerminalStrValue(trpProp.Object);
				}

				// Aspect is another special case
				if (sammInfo.SammInst is Samm.Aspect siAspect)
				{
					siAspect.Namespaces = globalNamespaces;
					siAspect.Comments = globalComments;

					AutoFillHeadId = idSet.DefaultInstanceURN;
					var afid = globalNamespaces.ExtendUri(":");
					if (afid?.HasContent() == true)
						AutoFillHeadId = afid;
				}

				// after this, the sammInst is fine; we need to prepare the outside
				ImportCreateCDandIds(
					env, idSet, sammInfo,
					givenUriId: RdfHelper.GetTerminalStrValue(trpSammElem.Subject),
					overIdShort: elemName,
					cdDesc: cdDesc?.Cast<Aas.ILangStringTextType>().ToList());
			}
		}

		public IEnumerable<SammReference> AllSammReferences(Samm.ModelElement me)
		{
			// access
			if (me == null)
				yield break;

			// reflection
			foreach (var pi in me.GetType().GetProperties())
			{
				if (pi.PropertyType.IsAssignableTo(typeof(List<SammReference>)))
				{
					var lsr = pi.GetValue(me) as List<SammReference>;
					if (lsr != null)
						foreach (var sr in lsr)
							yield return sr;
				}

				if (pi.PropertyType.IsAssignableTo(typeof(List<OptionalSammReference>)))
				{
					var lsr = pi.GetValue(me) as List<OptionalSammReference>;
					if (lsr != null)
						foreach (var sr in lsr)
							yield return sr;
				}

				if (pi.PropertyType.IsAssignableTo(typeof(SammReference)))
				{
					var sr = pi.GetValue(me) as SammReference;
					if (sr != null)
						yield return sr;
				}
			}
		}

		protected Dictionary<string, string> _visitedCdIds = new System.Collections.Generic.Dictionary<string, string>();

		protected INode ExportSammOptionalReference(
			Aas.IEnvironment env,
			IGraph g,
			SammIdSet idSet,
			Samm.Aspect asp,
			Samm.OptionalSammReference osr,
			string contentRelationshipUri)
		{
			// access
			if (g == null || idSet == null || asp?.Namespaces == null || osr == null)
				return null;

			// make a blank node
			var orNode = g.CreateBlankNode();

			// add property
			g.Assert(new Triple(
				orNode,
				g.CreateUriNode(new Uri(contentRelationshipUri)),
				g.CreateUriNode(asp.Namespaces.PrefixUri(osr.Value))));

			// add optional
			g.Assert(new Triple(
				orNode,
				g.CreateUriNode(new Uri(idSet.RdfCollOptional)),
				g.CreateLiteralNode(
					osr.Optional ? "true" : "false",
					datatype: new Uri(idSet.XsdBoolean))));

			// result
			return orNode;
		}

		protected INode ExportRdfCollection<T>(
			Aas.IEnvironment env,
			IGraph g,
			SammIdSet idSet,
			Samm.Aspect asp,
			List<T> coll,
			Func<T, INode> lambdaCreateContentNode)
		{
			// access
			if (g == null || idSet == null || asp?.Namespaces == null || coll == null)
				return null;

			// make a blank node and start
			var startNode = g.CreateBlankNode();
			var currNode = startNode;
			var collI = 0;

			while (collI < coll.Count())
			{			
				// make the content node and link it via "first"
				var contentNode = lambdaCreateContentNode?.Invoke(coll[collI]);
				if (contentNode != null)
				{
					g.Assert(
						currNode,
						g.CreateUriNode(new Uri(idSet.RdfCollFirst)),
						contentNode);
				}

				// make the "rest" target node either as "nil" or next node
				if (collI < coll.Count() - 1)
				{
					// next node
					var nextNode = g.CreateBlankNode();

					// link to this
					g.Assert(
						currNode,
						g.CreateUriNode(new Uri(idSet.RdfCollRest)),
						nextNode);

					// increment
					currNode = nextNode;
					collI++;
				}
				else
				{
					// finalize
					g.Assert(
						currNode,
						g.CreateUriNode(new Uri(idSet.RdfCollRest)),
						g.CreateUriNode(new Uri(idSet.RdfCollNil)));
					break;
				}
			}

			return startNode;
		}

		public void ExportSammOneElement(
			Aas.IEnvironment env,
			IGraph g,
			SammIdSet idSet,
			Samm.Aspect asp,
			Aas.IConceptDescription cd,
			ModelElement me)
		{
			// access
			if (g == null || idSet == null || me == null || cd == null || asp?.Namespaces == null)
				return;

			// check self description and add triple type
			if (!(me is Samm.ISammSelfDescription ssd))
				return;
			var meUrn = ssd.GetSelfUrn(idSet.Version);
			if (meUrn?.HasContent() != true)
				return;

			// make a try catch to specifically report on errors
			try
			{
				// triple for subject "a" samm element
				var subjectNode = g.CreateUriNode(asp.Namespaces.PrefixUri(cd.Id));
				g.Assert(new Triple(
					subjectNode,
					g.CreateUriNode("rdf:type"),
					g.CreateUriNode(meUrn)));

				// add this node to the list of visited
				_visitedCdIds.Add(cd.Id, cd.Id);

				// special case: name
				g.Assert(new Triple(
					subjectNode,
					g.CreateUriNode(idSet.SammName),
					g.CreateLiteralNode(cd.IdShort)));

				// special case: description
				if (cd.Description != null && cd.Description.Count > 0)
					foreach (var ls in cd.Description)
					{
						g.Assert(new Triple(
							g.CreateUriNode(asp.Namespaces.PrefixUri(cd.Id)),
							g.CreateUriNode(idSet.SammDescription),
							RdfHelper.SafeCreateLiteralNode(g, ls.Text, ls.Language)));
					}

				// reflect
				foreach (var pi in me.GetType().GetProperties())
				{
					// the property needs to have a custom attribute to
					// identify the predicate uri of the turtle triples
					var propUri = Samm.Util.FindAnySammPropertyUriAttribute(pi, idSet.Version)?.Uri;
					if (propUri == null)
						continue;

					// some additional flags
					var propFlags = "" + pi.GetCustomAttribute<Samm.SammPropertyFlagsAttribute>()?.Flags;
					var propFlagsLC = propFlags.ToLower();

					// for nullables?
					var underlyingType = Nullable.GetUnderlyingType(pi.PropertyType);

					// just string
					if (pi.PropertyType.IsAssignableTo(typeof(string)))
					{
						var s = pi.GetValue(me) as string;
						var isUri = propFlagsLC.Contains("anyuri");
						if (s != null)
						{
							g.Assert(new Triple(
								g.CreateUriNode(asp.Namespaces.PrefixUri(cd.Id)),
								g.CreateUriNode(propUri),
								RdfHelper.CreateUriOrLiteralNode(g, idSet, s.ToSafeString(), isUri)));
						}
					}

					// just uint?
					if (pi.PropertyType.IsAssignableTo(typeof(uint?)))
					{
						var ui = (uint?)pi.GetValue(me);
						if (ui.HasValue)
						{
							// Note: for data type, it is important to use a absolute/ no-prefix URI
							g.Assert(new Triple(
								g.CreateUriNode(asp.Namespaces.PrefixUri(cd.Id)),
								g.CreateUriNode(propUri),
								g.CreateLiteralNode(ui.Value.ToSafeString(),
									datatype: new Uri(idSet.XsdNonNegInt))));
						}
					}

					// just any kind of enum?
					if (underlyingType != null && underlyingType.IsEnum)
					{
						// for the enum string representation, there is a prefix required
						var enumPrefix = pi.GetCustomAttribute<Samm.SammPropertyPrefixAttribute>()?.Prefix;
						if (enumPrefix == null)
							continue;

						// current enum member
						var currEM = pi.GetValue(me);

						// generate a list of string representations
						var eMems = EnumHelper.EnumHelperGetMemberInfo(underlyingType).ToList();

						// find selected index
						int? selectedIndex = null;
						for (int emi = 0; emi < eMems.Count; emi++)
						{
							if (((int)eMems[emi].MemberInstance) == ((int)currEM))
								selectedIndex = emi;
						}

						// now add
						if (selectedIndex != null)
						{
							var objUri = enumPrefix + eMems[selectedIndex.Value].MemberValue;

							g.Assert(new Triple(
								g.CreateUriNode(asp.Namespaces.PrefixUri(cd.Id)),
								g.CreateUriNode(propUri),
								g.CreateUriNode(objUri)));
						}
					}

					// list of strings
					if (pi.PropertyType.IsAssignableTo(typeof(List<string>)))
					{
						var lls = pi.GetValue(me) as List<string>;
						var isUri = propFlagsLC.Contains("anyuri");
						if (lls != null)
							foreach (var ls in lls)
							{
								g.Assert(new Triple(
									g.CreateUriNode(asp.Namespaces.PrefixUri(cd.Id)),
									g.CreateUriNode(propUri),
									RdfHelper.CreateUriOrLiteralNode(g, idSet, ls.ToSafeString(), isUri)));
							}
					}

					// list of lang strings
					if (pi.PropertyType.IsAssignableTo(typeof(List<Samm.LangString>)))
					{
						var lls = pi.GetValue(me) as List<Samm.LangString>;
						if (lls != null)
							foreach (var ls in lls)
							{
								g.Assert(new Triple(
									g.CreateUriNode(asp.Namespaces.PrefixUri(cd.Id)),
									g.CreateUriNode(propUri),
									RdfHelper.SafeCreateLiteralNode(g, ls.Text, ls.Language)));
							}
					}

					// just a samm reference
					if (pi.PropertyType.IsAssignableTo(typeof(SammReference)))
					{
						var sr = pi.GetValue(me) as SammReference;
						if (sr != null && sr.Value?.HasContent() == true)
						{
							g.Assert(new Triple(
								g.CreateUriNode(asp.Namespaces.PrefixUri(cd.Id)),
								g.CreateUriNode(propUri),
								g.CreateUriNode(asp.Namespaces.PrefixUri(sr.Value))));
						}
					}

					// list of optional samm references
					if (pi.PropertyType.IsAssignableTo(typeof(List<OptionalSammReference>)))
					{
						// get the content elements
						var losr = pi.GetValue(me) as List<OptionalSammReference>;

						// need a special uri for each content element
						var collContentUri = pi.GetCustomAttribute<Samm.SammCollectionContentUriAttribute>()?.Uri;
						if (collContentUri == null)
							continue;

						if (false && losr != null && losr.Count == 1)
						{
							// use the normal approach for one optional reference
							;
						}

						if (losr != null && losr.Count >= 1)
						{
							// do a collection
							var collNode = ExportRdfCollection(env, g, idSet, asp, losr, (osr) =>
							{								
								if (osr.Optional == false)
									// direct content
									return g.CreateUriNode(
										asp.Namespaces.PrefixUri(osr?.Value.ToSafeString()));
								else
									// anonymous node
									return ExportSammOptionalReference(env, g, idSet, asp, osr,
										collContentUri);
							});
							if (collNode != null)
							{
								g.Assert(new Triple(
									g.CreateUriNode(asp.Namespaces.PrefixUri(cd.Id)),
									g.CreateUriNode(propUri),
									collNode));
							}
						}
					}

					// list of normal samm references
					if (pi.PropertyType.IsAssignableTo(typeof(List<SammReference>)))
					{
						var lsr = pi.GetValue(me) as List<SammReference>;
						if (lsr != null && lsr.Count >= 1)
						{
							// do a collection
							var collNode = ExportRdfCollection(env, g, idSet, asp, lsr, (sr) =>
							{
								return g.CreateUriNode(
									asp.Namespaces.PrefixUri(sr?.Value.ToSafeString()));
							});
							if (collNode != null)
							{
								g.Assert(new Triple(
									g.CreateUriNode(asp.Namespaces.PrefixUri(cd.Id)),
									g.CreateUriNode(propUri),
									collNode));
							}
						}
					}
				}
			} catch (Exception ex)
			{
				Log.Singleton.Error(ex, $"when creating graph elements for CD.Id {cd.Id}");
			}

			// try carefully to recurse
			foreach (var sr in AllSammReferences(me))
			{
				// visit only "new" ones
				var cd2 = env?.FindConceptDescriptionById(sr.Value) as ConceptDescription;
				var me2 = DispEditHelperSammModules.CheckReferableForSammElements(cd2).FirstOrDefault();
				if (cd2 != null && me2 != null)
					if (cd2?.Id?.HasContent() == true && !_visitedCdIds.ContainsKey(cd2.Id))
						ExportSammOneElement(env, g, idSet, asp, cd2, me2);
			}
		}

		public void ExportSammModelFromConceptDescription(
			Aas.IEnvironment env,
			Aas.IConceptDescription aspectCd,
			string fn)
		{
			// access
			if (env == null || aspectCd == null)
				return;

			// Reserve the graph, but make only by Aspect
			Graph g = null;

			// choose the right idSet
			var idSet = new SammIdSet();

			// Reserve text for comments
			string globalComments = "";
			Samm.Aspect globalAspect = null;

			// start with the Aspect
			var me1 = DispEditHelperSammModules.CheckReferableForSammElements(aspectCd).FirstOrDefault();
			if (me1 is Samm.Aspect asp)
			{
				// determine base uri
				var buri = asp.Namespaces.ExtendUri(":");
				if (buri?.HasContent() != true)
					buri = idSet.DefaultInstanceURN;

				// globals
				globalAspect = asp;
				globalComments = asp.Comments;

				// create Graph
				// see: https://dotnetrdf.org/docs/2.7.x/user_guide/Working-With-Graphs.html
				g = new Graph();
				g.BaseUri = new Uri(buri);

				// create namespace map
				if (asp.Namespaces != null)
					for (int ni = 0; ni < asp.Namespaces.Count(); ni++)
					{
						var nit = asp.Namespaces[ni];
						g.NamespaceMap.AddNamespace(
							nit.Prefix?.TrimEnd(':'),
							new Uri(nit.Uri));
					}

				// hack
				g.NamespaceMap.AddNamespace("this", new Uri(buri));

				// export
				ExportSammOneElement(env, g, idSet, globalAspect, aspectCd, asp);
			}
			else
				Log.Singleton.Error($"ConceptDescription {aspectCd?.IdShort} is missing SAMM model element " +
					$"for Aspect. Cannot continue!");

			// ok to go on?
			if (g == null)
			{
				Log.Singleton.Error("No graph found to be exported. Aborting!");
				return;
			}

			// save as string
			CompressingTurtleWriter rdfWriter = new CompressingTurtleWriter(TurtleSyntax.W3C);
			rdfWriter.HighSpeedModePermitted = false;
			String globalGraph = VDS.RDF.Writing.StringWriter.Write(g, rdfWriter);

			// build the whole file
			var textAll = globalComments
				+ System.Environment.NewLine
				+ System.Environment.NewLine
				+ globalGraph;
			System.IO.File.WriteAllText(fn, textAll);
		}
	}


	public class SammIdfTuple
	{
		public Aas.IConceptDescription CD;
		public Samm.ModelElement ME;

		public SammIdfTuple() { }

		public SammIdfTuple(
			Aas.IConceptDescription cd,
			Samm.ModelElement me)
		{
			CD = cd;
			ME = me;
		}
	}

	/// <summary>
	/// Dedicated <c>IdentifiableLookupStore</c> for <c>Samm.ModelElements</c> in <c>ConceptDescriptions.</c>
	/// </summary>
	public class SammModelElementLookupStore : PackageCentral.IdentifiableLookupStore<Aas.IConceptDescription, SammIdfTuple>
	{
		/// <summary>
		/// Lookup first element for id <c>idKey</c> and 
		/// return the result of the given lambda selection.
		/// Note: just a shortcut to <c>LookupFirstResult()</c>
		/// </summary>
		public SammIdfTuple Lookup(SammReference sr)
		{
			return LookupFirstResult(sr?.Value);
		}

		/// <summary>
		/// Lookup first element for id <c>idKey</c> and 
		/// return the result of the given lambda selection.
		/// Note: just a shortcut to <c>LookupFirstResult()</c>
		/// </summary>
		public T Lookup<T>(SammReference sr) where T : Samm.ModelElement
		{
			return LookupFirstResult(sr?.Value) as T;
		}

		public IEnumerable<SammIdfTuple> LookupFor(IEnumerable<Samm.SammReference> references)
		{
			// access
			if (references == null)
				yield break;

			// translare
			foreach (var inref in references)
			{
				yield return LookupFirstResult(inref?.Value);
			}
		}
	}

	/// <summary>
	/// This class provides transformation from SAMM models to other models, 
	/// e.g Submodel instances.
	/// </summary>
	public class SammTransformation
	{
		public Aas.IReference CreateSemanticId(string id)
		{
			return new Aas.Reference(
				ReferenceTypes.ExternalReference, 
				(new Aas.IKey[] { 
					new Aas.Key(KeyTypes.GlobalReference, id)
				}).ToList());
		}

		public void CreateSubmodelElementsInto(
			Aas.IEnvironment env,
			SammModelElementLookupStore store,
			Samm.Aspect aspect,
			List<Aas.ISubmodelElement> aasElems,
			IEnumerable<OptionalSammReference> sammElems)
		{
			// access
			if (env == null || aasElems == null || sammElems == null || aspect == null)
				return;

			// iterate over SAMM properties 
			foreach (var osrProp in sammElems)
			{
				var sitProp = store.Lookup(osrProp);
				var meProp = sitProp?.ME as Samm.Property;
				if (meProp == null)
					continue;

				// keep track for later use
				Aas.ISubmodelElement addedElem = null;
				var qualiferToAdd = new List<Aas.IQualifier>();

				// may be already 1st qualifer
				if (osrProp.Optional)
					// Cardinality
					qualiferToAdd.Add(
						AasSmtQualifiers.CreateQualifierSmtCardinality(AasSmtQualifiers.SmtCardinality.ZeroToOne));

				// example value directly associated with the property
				if (meProp.ExampleValue != null)
					// ExampleValue
					qualiferToAdd.Add(
						AasSmtQualifiers.CreateQualifierSmtExampleValue(meProp.ExampleValue));

				// ok, a Submodel element shall be created.
				// But more details (SMC? Property?) are only avilable via 
				// Characteristic -> dataType ..
				var sitChar = store.Lookup(meProp.Characteristic);
				var meChar = sitChar?.ME as Samm.Characteristic;
				var meDt = store.Lookup(meChar?.DataType);
				if (meDt != null && meDt.ME is Samm.Entity meDtEnt)
				{
					// Characteristic -> dataType goes to an entity
					// make a SMC and add directly
					var newSmc = new Aas.SubmodelElementCollection(
						idShort: "" + sitProp.CD.IdShort,
						semanticId: CreateSemanticId(meDt.CD.Id),
						value: new List<ISubmodelElement>());
					addedElem = newSmc;
					aasElems.Add(newSmc);

					// recurse into it ..
					CreateSubmodelElementsInto(
						env, store, aspect,
						newSmc.Value,
						meDtEnt.Properties);
				}				
				else
				{
					// if in doubt, create a Property with xsd:string
					Aas.DataTypeDefXsd valueType = DataTypeDefXsd.String;

					// get a "handy" uri from Characteristic -> dataType
					var dataTypeChar = aspect.Namespaces?.PrefixUri(meChar?.DataType?.Value);
					if (dataTypeChar?.StartsWith("xsd:") == true)
					{
						// be a bit carefull with de-serialization
						try
						{
							dataTypeChar = dataTypeChar.Replace("xsd:", "xs:");
							var x = Aas.Stringification.DataTypeDefXsdFromString(dataTypeChar);
							if (x.HasValue)
								valueType = x.Value;
						}
						catch (Exception ex)
						{
							Log.Singleton.Error(ex, $"when using XSD datatype {dataTypeChar}");
						}
					}

					// make a Property
					var newProp = new Aas.Property(
						valueType: valueType,
						semanticId: CreateSemanticId(sitProp.CD.Id),
						idShort: "" + sitProp.CD?.IdShort);
					addedElem = newProp;
					aasElems.Add(newProp);
				}

				// add further information
				
				if (sitChar?.ME is Samm.Trait charTrait)
				{
					foreach (var sitCons in store.LookupFor(charTrait.Constraint))
					{
						if (sitCons.ME is Samm.RegularExpressionConstraint regexCons)
						{
							// AllowedValue == Regex
							qualiferToAdd.Add(
								AasSmtQualifiers.CreateQualifierSmtAllowedValue(regexCons.Value));
						}

						if (sitCons.ME is Samm.LanguageConstraint langCons)
						{
							// RequiredLanguage
							qualiferToAdd.Add(
								AasSmtQualifiers.CreateQualifierSmtRequiredLang(langCons.LanguageCode));
						}
					}
				}

				if (sitChar?.ME is Samm.State charState)
				{
					if (charState.DefaultValue?.HasContent() == true)
						// Default value .. only for States
						qualiferToAdd.Add(
							AasSmtQualifiers.CreateQualifierSmtDefaultValue(charState.DefaultValue));
				}		

				// elaborate added element further
				if (addedElem != null && qualiferToAdd.Count > 0)
					addedElem.Qualifiers = qualiferToAdd;
			}
		}

		public void CreateSubmodelInstanceFromAspectCD(
			Aas.IEnvironment env,
			Aas.IConceptDescription cdAspect)
		{
			// access
			if (env?.ConceptDescriptions == null || cdAspect == null)
				return;

			// create store
			var store = new SammModelElementLookupStore();
			store.StartDictionaryAccess(
				new[] { env.ConceptDescriptions }, 
				lambdaSelectResult: (cd) => {
					var me = DispEditHelperSammModules.CheckReferableForSammElements(cd).FirstOrDefault();
					return new SammIdfTuple(cd, me);
				});

			// access Aspect
			if (!(store.Lookup(cdAspect.Id)?.ME is Samm.Aspect meAspect))
			{
				Log.Singleton.Error("Cannot access the SAMM Aspect. Aborting!");
				return;
			}

			// create Submodel
			var submodel = new Aas.Submodel(
					idShort: "From_SAMM_" + cdAspect.IdShort,
					id: "" + AdminShellUtil.GenerateIdAccordingTemplate(Options.Curr.TemplateIdSubmodelInstance),
					description: cdAspect.Description?.Copy(),
					semanticId: CreateSemanticId(cdAspect.Id),
					submodelElements: new List<ISubmodelElement>());

			// iterate over elements
			CreateSubmodelElementsInto(
				env, store, meAspect,
				submodel.SubmodelElements,
				meAspect.Properties);

			// add Submodel
			env.Add(submodel);

			// for convenience, add to first aas
			var firstAas = env.AssetAdministrationShells?.FirstOrDefault();
			if (firstAas != null)
				firstAas.AddSubmodelReference(
					submodel.GetModelReference());
		}

        public static bool ConceptSuitableForSubmodelCreate(ModelElement me)
        {
            // access
            if (me == null)
                return false;

            // ok?
            if (me is Samm.Aspect asp)
                return true;

            return false;
        }

        public static IEnumerable<DispEditHelperMiniModules.ConceptOrganizedChildItem> 
			FindChildElementsForConcept(
				PackageCentral.PackageCentral packages,
				Aas.IConceptDescription cd,
				ModelElement me)
		{
			// access
			if (packages == null || cd == null || me == null)
				yield break;

			// possible childs?
			var childs = new List<Samm.SammReference>();

			// lambda for going deeper (Property) -> Characteristics -> Entity -> Property
			Func<Samm.Property, Tuple<Samm.Characteristic, Samm.Entity>> lambdaTryProp2Entity = (propTest) =>
			{
				var propCharCd = packages.QuickLookupFirstIdent<Aas.IConceptDescription>(propTest.Characteristic?.Value);
				var propCharMe = DispEditHelperSammModules.CheckReferableForSammElements(propCharCd)?.FirstOrDefault();
				if (propCharMe is Samm.Characteristic propChar)
				{
					var propEntCd = packages.QuickLookupFirstIdent<Aas.IConceptDescription>(propChar.DataType?.Value);
					var propEntMe = DispEditHelperSammModules.CheckReferableForSammElements(propEntCd)?.FirstOrDefault();
					if (propEntMe is Samm.Entity ent)
						return new Tuple<Characteristic, Samm.Entity>(propChar, ent);
				}
				return null;
			};
			
			// Aspect
			if (me is Samm.Aspect asp)
			{
				if (asp.Properties != null)
					childs.AddRange(asp.Properties);
				if (asp.Operations != null)
					childs.AddRange(asp.Operations);
				if (asp.Events != null)
					childs.AddRange(asp.Events);
			}

			// Property
			if (me is Samm.Property prop)
			{
				var propEnt = lambdaTryProp2Entity(prop);
				if (propEnt?.Item2?.Properties != null)
                    foreach (var p in propEnt.Item2.Properties)
                        childs.Add(p);
            }

			// try lookup childs
			foreach (var child in childs)
			{
				// access
				var childCd = packages.QuickLookupFirstIdent<Aas.IConceptDescription>(child?.Value);
				var childMe = DispEditHelperSammModules.CheckReferableForSammElements(childCd)?.FirstOrDefault();
				
				if (childMe is Samm.Property childProp)
				{
					// for the time being, fake a SmtRecord 
					var smtRec = new SmtAttributeRecord();

					// poor mens cardinality
					smtRec.SmeCardinality = AasSmtQualifiers.SmtCardinality.One;
					if (child is OptionalSammReference osr && osr.Optional)
						smtRec.SmeCardinality = AasSmtQualifiers.SmtCardinality.ZeroToOne;

					// poor mens initial / default / example value
					smtRec.ExampleValue = childProp.ExampleValue;

					// poor mens SME type
					var childSmeType = AasSubmodelElements.Property;

					// test for MLP?
					if (SammIdSets.IsCharInstMultiLanguage(childProp.Characteristic?.Value))
						childSmeType = AasSubmodelElements.MultiLanguageProperty;

					// test for SMC/ SML?
					var smlNoOrderRelevant = false;
					var childEntTest = lambdaTryProp2Entity(childProp);
					if (childEntTest != null)
					{
						childSmeType = AasSubmodelElements.SubmodelElementCollection;
						
						if (childEntTest.Item1 is Samm.Set
							|| childEntTest.Item1 is Samm.Collection
							|| childEntTest.Item1 is Samm.List
							|| childEntTest.Item1 is Samm.SortedSet
							|| childEntTest.Item1 is Samm.TimeSeries)
							childSmeType = AasSubmodelElements.SubmodelElementList;

						if (childEntTest.Item1 is Samm.Set
							|| childEntTest.Item1 is Samm.Collection)
							smlNoOrderRelevant = true;
					}
                    smtRec.SubmodelElements = new List<AasSubmodelElements>() { childSmeType };

					// put into item
					yield return new DispEditHelperMiniModules.ConceptOrganizedChildItem()
					{
						Cd = childCd,
						Card = smtRec.SmeCardinality,
						SmlNoOrderRelevant = smlNoOrderRelevant,
						SmtRec = smtRec 
					};
				}
			}
		}
	}
}
