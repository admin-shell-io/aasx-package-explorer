/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasCore.Samm2_2_0;
using AasxIntegrationBase;
using AdminShellNS;
using Aml.Engine.CAEX;
using AnyUi;
using Extensions;
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xaml;
using static AasxPackageLogic.AasSmtQualifiers;
using Aas = AasCore.Aas3_0;
using Samm = AasCore.Samm2_2_0;


namespace AasxPackageLogic
{
	/// <summary>
	/// This class extends the AAS meta model editing function for those related to
	/// SAMM (Semantic Aspect Meta Model) elements. 
	/// </summary>
	public class DispEditHelperExtensions : DispEditHelperMiniModules
	{
		public static void GeneralExtensionHelperUpdateJson(Aas.IExtension se, Type recType, ExtensionRecordBase recInst)
		{
			// trivial
			if (se == null || recType == null || recInst == null)
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
				json = JsonConvert.SerializeObject(recInst, recType, settings);
			}
			catch (Exception ex)
			{
				LogInternally.That.SilentlyIgnoredError(ex);
			}

			// save this to the extension
			se.Value = json;
			se.ValueType = DataTypeDefXsd.String;
		}

		public static bool GeneralExtensionHelperAddJsonExtension(
			Aas.IHasExtensions ihe, Type recType, ExtensionRecordBase recInst)
		{
			// acces
			if (ihe == null || recType == null || recInst == null
				|| !(recInst is IExtensionSelfDescription ssd))
				return false;

			// create extension
			var newExt = new Aas.Extension(
				name: ssd.GetSelfName(),
				semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
					(new[] {
						new Aas.Key(KeyTypes.GlobalReference,
						"" + ssd.GetSelfUri())
					})
					.Cast<Aas.IKey>().ToList()),
				value: "");

			// add JSON
			GeneralExtensionHelperUpdateJson(newExt, recType, recInst);

			// add to extension
			ihe.Extensions = ihe.Extensions ?? new List<IExtension>();
			ihe.Extensions.Add(newExt);

			// ok
			return true;
		}

		/// <summary>
		/// Shall provide rather quick access to information ..
		/// </summary>
		public static ExtensionRecordBase CheckReferableForExtensionRecordType(
			Aas.IReferable rf,
			Type[] typesAllowed = null)
		{
			// access
			if (rf?.Extensions == null)
				return null;

			// find any?
			foreach (var se in rf.Extensions)
				if (se.SemanticId?.IsValid() == true && se.SemanticId.Keys.Count == 1)
				{
					var t = ExtensionRecords.GetTypeInstFromUri(se.SemanticId.Keys[0].Value);
					if (t != null)
						return t;
				}

			// no?
			return null;
		}

		public static ExtensionRecordBase CheckReferableForSingleExtensionRecord(
			Aas.IExtension se,
			bool allowCreateOnNull = false)
		{
			// access
			if (se == null || se.SemanticId?.IsValid() != true
				|| se.SemanticId.Keys.Count < 1)
				return null;

			// get type 
			var recTypeInst = ExtensionRecords.GetTypeInstFromUri(se.SemanticId.Keys[0].Value);
			if (recTypeInst == null)
				return null;

			// get instance data
			ExtensionRecordBase recInst = null;

			// try to de-serializan extension value
			try
			{
				if (se.Value != null)
					recInst = JsonConvert.DeserializeObject(se.Value, recTypeInst.GetType()) as ExtensionRecordBase;
			}
			catch (Exception ex)
			{
				LogInternally.That.SilentlyIgnoredError(ex);
				recInst = null;
			}

			// if not, may create new
			if (recInst == null && allowCreateOnNull)
			{
				recInst = Activator.CreateInstance(recTypeInst.GetType(), new object[] { }) as ExtensionRecordBase;
			}

			// no?
			if (recInst == null)
				return null;

			// give back
			return recInst;
		}

		public static IEnumerable<ExtensionRecordBase> CheckReferableForExtensionRecords(
			Aas.IReferable rf,
			Type[] typesAllowed = null)
		{
			// access
			if (rf?.Extensions == null)
				yield break;

			// find any?
			foreach (var se in rf.Extensions)
			{
				var rec = CheckReferableForSingleExtensionRecord(se);

				if (typesAllowed != null)
				{
					var found = false;
					foreach (var ta in typesAllowed)
						if (ta == rec.GetType())
							found = true;
					if (!found)
						continue;
				}

				if (rec != null)
					yield return rec;
			}
		}

		public static IEnumerable<T> CheckReferableForExtensionRecords<T>(
			Aas.IReferable rf) where T : class
		{
			foreach (var x in CheckReferableForExtensionRecords(rf, new[] { typeof(T) }))
				yield return x as T;
		}

		protected void ExtensionHelperAddScalarField
			(AnyUiStackPanel stack,
			PropertyInfo pii,
			ExtensionRecordBase recInst,
			Action<ExtensionRecordBase> setRecInst,
			bool isNullable,
			Func<object, string> toStringRepr,
			Func<string, object> fromStrRepr) 
		{
			// access
			if (stack == null || pii == null || recInst == null)
				return;

			// 1 line
			AddKeyValueExRef(
				stack, "" + pii.Name, recInst,
				value: "" + toStringRepr?.Invoke(pii.GetValue(recInst)),
				null, repo,
				setValue: (v) =>
				{
					if (isNullable && (v == null || ((string)v).Trim().Length < 1))
						pii.SetValue(recInst, null);
					else
					{
						var result = fromStrRepr?.Invoke((string)v);
						if (result != null)
							pii.SetValue(recInst, result);
					}
					setRecInst?.Invoke(recInst);
					return new AnyUiLambdaActionNone();
				},
				maxLines: 1);
		}

		public void ExtensionHelperAddEditFieldsByReflection(
			Aas.Environment env,
			AnyUiStackPanel stack,
			ExtensionRecordBase recInst,
			Aas.IReferable relatedReferable,
			Action<ExtensionRecordBase> setValue)
		{
			// access
			if (env == null || stack == null || recInst == null)
				return;

			// visually ease
			this.AddVerticalSpace(stack);

			// okay, try to build up a edit field by reflection
			var propInfo = recInst.GetType().GetProperties();
			for (int pi = 0; pi < propInfo.Length; pi++)
			{
				// access
				var pii = propInfo[pi];

				// some type investigation
				var propType = pii.PropertyType;
				var underlyingType = Nullable.GetUnderlyingType(propType);

				// make hint lambda
				Action<bool> hintLambda = (hint) =>
				{
					var hintAttr = pii.GetCustomAttribute<ExtensionHintAttributeAttribute>();
					if (hintAttr == null)
						return;
					this.AddHintBubble(stack, hintMode, new[] {
					new HintCheck(
						() => hint,
						text: hintAttr.HintText,
						severityLevel: HintCheck.Severity.Notice)
					});
				};
			
				// List of LangString
				if (pii.PropertyType.IsAssignableTo(typeof(List<Aas.LangStringTextType>)))
				{
					// space
					this.AddVerticalSpace(stack);

					// get data
					var lls = (List<Aas.LangStringTextType>)pii.GetValue(recInst);

					// hint?
					hintLambda(lls == null || lls.Count < 1);

					// handle null
					Action<List<Aas.ILangStringTextType>> lambdaSetValue = (v) =>
					{
						var back = v?.Select((ls) => new Aas.LangStringTextType(ls.Language, ls.Text)).ToList();
						pii.SetValue(recInst, back);
						setValue?.Invoke(recInst);
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

				// List of string?
				if (pii.PropertyType.IsAssignableTo(typeof(List<string>)))
				{
					this.AddVerticalSpace(stack);

					var ls = (List<string>)pii.GetValue(recInst);

					// hint?
					hintLambda(ls == null || ls.Count < 1);

					if (this.SafeguardAccess(stack, repo, ls, "" + pii.Name + ":",
						"Create data element!",
						v =>
						{
							pii.SetValue(recInst, (new List<Aas.ILangStringTextType>()));
							setValue?.Invoke(recInst);
							return new AnyUiLambdaActionRedrawEntity();
						}))
					{

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
									pii.SetValue(recInst, ls);
									setValue?.Invoke(recInst);
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
										pii.SetValue(recInst, ls);
										setValue?.Invoke(recInst);
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
										pii.SetValue(recInst, ls);
										setValue?.Invoke(recInst);
										return new AnyUiLambdaActionRedrawEntity();
									});
						}
					}
				}

				// single string?
				if (pii.PropertyType.IsAssignableTo(typeof(string)))
				{
					var isMultiLineAttr = pii.GetCustomAttribute<ExtensionMultiLineAttribute>();

					// value and hint?
					var strVal = (string)pii.GetValue(recInst);
					hintLambda(strVal == null || strVal.Length < 1);

					Func<object, AnyUiLambdaActionBase> setValueLambda = (v) =>
					{
						pii.SetValue(recInst, v);
						setValue?.Invoke(recInst);
						return new AnyUiLambdaActionNone();
					};

					if (isMultiLineAttr == null)
					{
						// 1 line
						AddKeyValueExRef(
							stack, "" + pii.Name, recInst, strVal, null, repo,
							setValue: setValueLambda);
					}
					else
					{
						// makes sense to have a bit vertical space
						AddVerticalSpace(stack);

						// multi line
						AddKeyValueExRef(
							stack, "" + pii.Name, recInst, (string)pii.GetValue(recInst), null, repo,
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
										text: (string)pii.GetValue(recInst));
									if (this.context.StartFlyoverModal(uc))
									{
										pii.SetValue(recInst, uc.Text);
										setValue?.Invoke(recInst);
										return new AnyUiLambdaActionRedrawEntity();
									}
								}
								return new AnyUiLambdaActionNone();
							});
					}
				}

				// scalar value type
				// Note: for Double, "G17" shall be used according to Microsoft; this is changed
				// to "G16" in order to round properly before least-significant-bit-precision errors.

				if (underlyingType != null)
				{
					if (pii.PropertyType.IsAssignableTo(typeof(uint?)))
						ExtensionHelperAddScalarField(
							stack, pii, recInst, setValue,
							isNullable: true,
							toStringRepr: (o) =>
							{
								var val = (uint?)o;
								return (val.HasValue) ? val.Value.ToString() : null;
							},
							fromStrRepr: (s) => { if (uint.TryParse(s, out var res)) return res; else return null; });

					if (pii.PropertyType.IsAssignableTo(typeof(int?)))
						ExtensionHelperAddScalarField(
							stack, pii, recInst, setValue,
							isNullable: true,
							toStringRepr: (o) =>
							{
								var val = (int?)o;
								return (val.HasValue) ? val.Value.ToString() : null;
							},
							fromStrRepr: (s) => { if (int.TryParse(s, out var res)) return res; else return null; });

					if (pii.PropertyType.IsAssignableTo(typeof(double?)))
						ExtensionHelperAddScalarField(
							stack, pii, recInst, setValue,
							isNullable: true,
							toStringRepr: (o) =>
							{
								var val = (double?)o;
								return (val.HasValue) ? val.Value.ToString("G16", CultureInfo.InvariantCulture) : null;
							},
							fromStrRepr: (s) => { 
								if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var res)) 
									return res; else return null; });
				}
				
				if (pii.PropertyType.IsAssignableTo(typeof(uint)))
					ExtensionHelperAddScalarField(
						stack, pii, recInst, setValue,
						isNullable: false,
						toStringRepr: (o) => ((uint)o).ToString(),
						fromStrRepr: (s) => { if (uint.TryParse(s, out var res)) return res; else return null; });

				if (pii.PropertyType.IsAssignableTo(typeof(int)))
					ExtensionHelperAddScalarField(
						stack, pii, recInst, setValue,
						isNullable: false,
						toStringRepr: (o) => ((int)o).ToString(),
						fromStrRepr: (s) => { if (int.TryParse(s, out var res)) return res; else return null; });

				if (pii.PropertyType.IsAssignableTo(typeof(double)))
					ExtensionHelperAddScalarField(
						stack, pii, recInst, setValue,
						isNullable: false,
						toStringRepr: (o) => ((double)o).ToString("G16", CultureInfo.InvariantCulture),
						fromStrRepr: (s) => { 
							if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var res)) 
								return res; else return null; });

				// nullable enum?
				var typeForEnum = propType;
				if (underlyingType != null && underlyingType.IsEnum)
					typeForEnum = underlyingType;
				if (typeForEnum.IsEnum)
				{
					// a little space
					AddVerticalSpace(stack);

					// current enum member
					var currEM = pii.GetValue(recInst);

					// generate a list for combo box
					var eMems = EnumHelper.EnumHelperGetMemberInfo(typeForEnum).ToList();

					// find selected index
					int? selectedIndex = null;
					if (currEM != null)
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
								new AnyUiThickness(0, 1, 2, 3),
								AnyUiContextCapability.Blazor, new AnyUiThickness(4, 2, 2, 0)),
							padding: NormalOrCapa(
								new AnyUiThickness(2, 1, 2, 1),
								AnyUiContextCapability.Blazor, new AnyUiThickness(0, 4, 0, 4)),
							selectedIndex: selectedIndex,
							items: eMems.Select((mi) => mi.MemberDisplay).ToArray()),
						setValue: (o) =>
						{
							if (cb.SelectedIndex.HasValue
								&& cb.SelectedIndex.Value >= 0
								&& cb.SelectedIndex.Value < eMems.Count)
							{
								pii.SetValue(recInst, eMems[cb.SelectedIndex.Value].MemberInstance);
								setValue?.Invoke(recInst);
							}
							return new AnyUiLambdaActionNone();
						});
				}
			}
		}

		public void DisplayOrEditEntityExtensionRecords(
			Aas.Environment env, 
			AnyUiStackPanel stack,
			List<Aas.IExtension> extension,
			Action<List<Aas.IExtension>> setOutput,
			string[] addPresetNames = null, List<Aas.IKey>[] addPresetKeyLists = null,
			Aas.IReferable relatedReferable = null,
			AasxMenu superMenu = null)
		{
			// access
			if (stack == null)
				return;

			// members
			this.AddGroup(stack, "Known extensions \u00ab experimental \u00bb :", levelColors.MainSection);

			this.AddHintBubble(
				stack, hintMode,
				new[] {
					new HintCheck(
						() => { return extension == null ||
							extension.Count < 1; },
						"For modelling Submodel template specifications (SMT), a set of particular attributes " +
						"to the elements of SMTs are specified. These attributes can be added as specific " +
						"Qualifiers or via adding an extension as a whole.",
						breakIfTrue: true, severityLevel: HintCheck.Severity.Notice),
				});
			if (this.SafeguardAccess(
					stack, this.repo, extension, "Known extensions:", "Create data element!",
					v =>
					{
						setOutput?.Invoke(new List<Aas.IExtension>());
						return new AnyUiLambdaActionRedrawEntity();
					}))
			{
				// head control
				if (editMode)
				{
					// let the user control the number of references
					this.AddActionPanel(
						stack, "Known extension:", repo: repo,
						superMenu: superMenu,
						ticketMenu: new AasxMenu()
							.AddAction("add-smt-attributes", "SMT attributes",
								"Add attributes for Submodel template specifications.")
							.AddAction("delete-last", "Delete last extension",
								"Deletes last extension."),
						ticketAction: (buttonNdx, ticket) =>
						{
							if (buttonNdx == 0)
							{							
								// new
								var newSet = new SmtAttributeRecord();

								// now add
								extension.Add(
									new Aas.Extension(
										name: newSet.GetSelfName(),
										semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
											(new[] {
												new Aas.Key(KeyTypes.GlobalReference,
												"" + newSet.GetSelfUri())
											})
											.Cast<Aas.IKey>().ToList()),
										value: ""));
							}
							
							// remove
							if (buttonNdx == 1)
							{
								if (extension.Count > 0)
									extension.RemoveAt(extension.Count - 1);
								else
									setOutput?.Invoke(null);
							}

							this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
							return new AnyUiLambdaActionRedrawEntity();
						});
				}

				// now use the normal mechanism to deal with editMode or not ..
				for (int exti = 0; exti < extension.Count; exti++)
				{
					// get
					var se = extension[exti];

					// get instance data
					ExtensionRecordBase recInst = CheckReferableForSingleExtensionRecord(
						se, allowCreateOnNull: true);
					if (!(recInst is IExtensionSelfDescription esd))
						continue;

					// icon element?
					AnyUiFrameworkElement iconElem = null;
					var ri = (recInst as IExtensionSelfDescription)?.GetRenderInfo();
					if (ri != null)
						iconElem = new AnyUiBorder()
						{
							Background = new AnyUiBrush(ri.Background),
							BorderBrush = new AnyUiBrush(ri.Foreground),
							BorderThickness = new AnyUiThickness(2.0f),
							CornerRadius = 2.0,
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

					// make visual group
					this.AddGroup(stack, $"Known extension [{exti + 1}]: {esd.GetSelfName()}",
						levelColors.SubSection.Bg, levelColors.SubSection.Fg, iconElement: iconElem);

					// edit
					ExtensionHelperAddEditFieldsByReflection(
						env, stack,
						recInst: recInst,
						relatedReferable: relatedReferable,
						setValue: (si) =>
						{
							GeneralExtensionHelperUpdateJson(se, si.GetType(), si);
						});
				}
			}
		}		
		
	}

	/// <summary>
	/// Holds information, how model element types should be rendered on the screen.
	/// </summary>
	public class ExtensionRecordRenderInfo
	{
		public string DisplayName = "";
		public string Abbreviation = "";
		public uint Foreground = 0x00000000;
		public uint Background = 0x00000000;
	}

	/// <summary>
	/// Shall be implemented by all non-abstract model elements
	/// </summary>
	public interface IExtensionSelfDescription
	{
		/// <summary>
		/// get short name, which can als be used to distinguish elements.
		/// </summary>
		string GetSelfName();

		/// <summary>
		/// Get URN of this element class.
		/// </summary>
		string GetSelfUri();

		/// <summary>
		/// Return information to render elements visually on screen.
		/// </summary>
		/// <returns></returns>
		ExtensionRecordRenderInfo GetRenderInfo();
	}

	/// <summary>
	/// This class provides handles specific qualifiers, extensions
	/// for Submodel templates
	/// </summary>
	public static class AasSmtQualifiers
	{
		/// <summary>
		/// Semantically different, but factually equal to <c>FormMultiplicity</c>
		/// </summary>
		public enum SmtCardinality
		{
			[EnumMember(Value = "ZeroToOne")]
			[EnumMemberDisplay("ZeroToOne [0..1]")]
			ZeroToOne = 0,

			[EnumMember(Value = "One")]
			[EnumMemberDisplay("One [1]")]
			One,

			[EnumMember(Value = "ZeroToMany")]
			[EnumMemberDisplay("ZeroToMany [0..*]")]
			ZeroToMany,

			[EnumMember(Value = "OneToMany")]
			[EnumMemberDisplay("OneToMany [1..*]")]
			OneToMany
		};

		/// <summary>
		/// Specifies the user access mode for SubmodelElement instance.When a Submodel is 
		/// received from another party, if set to Read/Only, then the user shall not change the value.
		/// </summary>
		public enum AccessMode
		{
			[EnumMember(Value = "ReadWrite")]
			ReadWrite,

			[EnumMember(Value = "ReadOnly")]
			ReadOnly
		};

		public static Aas.IQualifier CreateQualifierSmtCardinality(SmtCardinality card)
		{
			return new Aas.Qualifier(
				type: "SMT/Cardinality",
				valueType: DataTypeDefXsd.String,
				kind: QualifierKind.TemplateQualifier,
				semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
					(new Aas.IKey[] {
						new Aas.Key(KeyTypes.GlobalReference,
							"https://admin-shell.io/SubmodelTemplates/Cardinality/1/0")
					}).ToList()),
				value: "" + card);
		}

		public static Aas.IQualifier CreateQualifierSmtAllowedValue(string regex)
		{
			return new Aas.Qualifier(
				type: "SMT/AllowedValue",
				valueType: DataTypeDefXsd.String,
				kind: QualifierKind.TemplateQualifier,
				semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
					(new Aas.IKey[] {
						new Aas.Key(KeyTypes.GlobalReference,
							"https://admin-shell.io/SubmodelTemplates/AllowedValue/1/0")
					}).ToList()),
				value: "" + regex);
		}

		public static Aas.IQualifier CreateQualifierSmtExampleValue(string exampleValue)
		{
			return new Aas.Qualifier(
				type: "SMT/ExampleValue",
				valueType: DataTypeDefXsd.String,
				kind: QualifierKind.TemplateQualifier,
				semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
					(new Aas.IKey[] {
						new Aas.Key(KeyTypes.GlobalReference,
							"https://admin-shell.io/SubmodelTemplates/ExampleValue/1/0")
					}).ToList()),
				value: "" + exampleValue);
		}

		public static Aas.IQualifier CreateQualifierSmtDefaultValue(string defaultValue)
		{
			return new Aas.Qualifier(
				type: "SMT/DefaultValue",
				valueType: DataTypeDefXsd.String,
				kind: QualifierKind.TemplateQualifier,
				semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
					(new Aas.IKey[] {
						new Aas.Key(KeyTypes.GlobalReference,
							"https://admin-shell.io/SubmodelTemplates/DefaultValue/1/0")
					}).ToList()),
				value: "" + defaultValue);
		}

		public static Aas.IQualifier CreateQualifierSmtEitherOr(string equivalencyClass)
		{
			return new Aas.Qualifier(
				type: "SMT/EitherOr",
				valueType: DataTypeDefXsd.String,
				kind: QualifierKind.TemplateQualifier,
				semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
					(new Aas.IKey[] {
						new Aas.Key(KeyTypes.GlobalReference,
							"https://admin-shell.io/SubmodelTemplates/Cardinality/1/0")
					}).ToList()),
				value: "" + equivalencyClass);
		}

		public static Aas.IQualifier CreateQualifierSmtRequiredLang(string reqLang)
		{
			return new Aas.Qualifier(
				type: "SMT/RequiredLang",
				valueType: DataTypeDefXsd.String,
				kind: QualifierKind.TemplateQualifier,
				semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
					(new Aas.IKey[] {
						new Aas.Key(KeyTypes.GlobalReference,
							"https://admin-shell.io/SubmodelTemplates/RequiredLang/1/0")
					}).ToList()),
				value: "" + reqLang);
		}

		public static Aas.IQualifier CreateQualifierSmtAccessMode(AccessMode mode)
		{
			return new Aas.Qualifier(
				type: "SMT/AccessMode",
				valueType: DataTypeDefXsd.String,
				kind: QualifierKind.TemplateQualifier,
				semanticId: new Aas.Reference(ReferenceTypes.ExternalReference,
					(new Aas.IKey[] {
						new Aas.Key(KeyTypes.GlobalReference,
							"https://admin-shell.io/SubmodelTemplates/AccessMode/1/0")
					}).ToList()),
				value: "" + mode);
		}

		public static Aas.IQualifier[] AllSmtQualifiers =
		{
			CreateQualifierSmtCardinality(SmtCardinality.One),
			CreateQualifierSmtAllowedValue(""),
			CreateQualifierSmtExampleValue(""),
			CreateQualifierSmtDefaultValue(""),
			CreateQualifierSmtEitherOr(""),
			CreateQualifierSmtRequiredLang(""),
			CreateQualifierSmtAccessMode(AccessMode.ReadWrite)
		};

		/// <summary>
		/// Find either <c>type</c> or <c>semanticId</c> and returns the link
		/// to a STATIC IQualifier (not to be changed!).
		/// </summary>
		public static Aas.IQualifier FindQualifierTypeInst(
			string type, Aas.IReference semanticId, bool relaxed = true)
		{
			// at best: tries to find semanticId
			Aas.IQualifier res = null;
			foreach (var qti in AllSmtQualifiers)
				if (semanticId?.IsValid() == true && semanticId.Matches(qti.SemanticId, MatchMode.Relaxed))
					res = qti;

			// do a more sloppy name comparison?
			if (relaxed && res == null && type?.HasContent() == true)
			{
				// some adoptions ahead
				type = type.Trim().ToLower();
				if (type == "cardinality")
					type = "smt/cardinality";
				if (type == "multiplicity")
					type = "smt/cardinality";

				// now try to find
				foreach (var qti in AllSmtQualifiers)
					if (qti.Type?.HasContent() == true
						&& qti.Type.Trim().ToLower() == type)
						res = qti;
			}

			// okay?
			return res;
		}

		public static SmtAttributeRecord FindSmtQualifiers(Aas.IReferable rf, bool removeQualifers = false)
		{
			// acesses
			if (!(rf is Aas.IQualifiable iqf))
				return null;

			// already done?
			if (iqf.Qualifiers == null || iqf.Qualifiers.Count() < 1)
				return null;

			// result
			SmtAttributeRecord rec = null;

			// try convert
			for (int qi = 0; qi < iqf.Qualifiers.Count(); qi++)
			{
				// find
				var qf = iqf.Qualifiers[qi];
				if (qf == null)
					continue;
				var qti = FindQualifierTypeInst(qf.Type, qf.SemanticId);
				if (qti == null)
					continue;

				// to convert
				rec = rec ?? new SmtAttributeRecord();

				if (qti.Type == "SMT/Cardinality")
					rec.Cardinality = (SmtCardinality)
							EnumHelper.GetEnumMemberFromValueString<SmtCardinality>(qf.Value);

				if (qti.Type == "SMT/EitherOr")
					rec.EitherOr = qf.Value;

				if (qti.Type == "SMT/InitialValue")
					rec.InitialValue = qf.Value;

				if (qti.Type == "SMT/DefaultValue")
					rec.DefaultValue = qf.Value;

				if (qti.Type == "SMT/ExampleValue")
					rec.ExampleValue = qf.Value;

				if (qti.Type == "SMT/AllowedRange")
					rec.AllowedRange = qf.Value;

				if (qti.Type == "SMT/AllowedIdShort")
					rec.AllowedIdShort = qf.Value;

				if (qti.Type == "SMT/AllowedValue")
					rec.AllowedValue = qf.Value;

				if (qti.Type == "SMT/RequiredLang")
					rec.RequiredLang = qf.Value;

				if (qti.Type == "SMT/AccessMode")
					rec.AccessMode = (AccessMode)
							EnumHelper.GetEnumMemberFromValueString<AccessMode>(qf.Value);

				// remove in qualfiers?
				if (removeQualifers)
					iqf.Qualifiers.RemoveAt(qi--);
			}

			// results
			return rec;
		}

		public static bool ConvertSmtQualifiersToExtension(Aas.IReferable rf)
		{
			// acesses
			if (!(rf is Aas.IQualifiable iqf) || !(rf is Aas.IHasExtensions ihe))
				return false;

			// already done?
			if (iqf.Qualifiers == null || iqf.Qualifiers.Count() < 1)
				return false;

			// convert
			SmtAttributeRecord rec = FindSmtQualifiers(rf, removeQualifers: true);

			// attach
			return DispEditHelperExtensions.GeneralExtensionHelperAddJsonExtension(rf, rec.GetType(), rec);
		}
	}

	/// <summary>
	/// This attribute gives a list of given presets to an field or property.
	/// in order to avoid cycles
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
	public class ExtensionHintAttributeAttribute : System.Attribute
	{
		public string HintText = "";

		public ExtensionHintAttributeAttribute(string hintText)
		{
			if (hintText != null)
				HintText = hintText;
		}
	}

	/// <summary>
	/// This attribute marks a string field/ property as multiline.
	/// in order to avoid cycles
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
	public class ExtensionMultiLineAttribute : System.Attribute
	{
		public int? MaxLines = null;

		public ExtensionMultiLineAttribute(int maxLines = -1)
		{
			if (maxLines > 0)
				MaxLines = maxLines;
		}
	}

	/// <summary>
	/// This attribute marks a string field/ property as multiline.
	/// in order to avoid cycles
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
	public class EnumMemberDisplayAttribute : System.Attribute
	{
		public string Text = "";

		public EnumMemberDisplayAttribute(string text)
		{
			if (text != null)
				Text = text;
		}
	}

	/// <summary>
	/// Abstract base class for information for data records of extensions.
	/// </summary>
	public class ExtensionRecordBase
	{		
	}

	/// <summary>
	/// Small class to report on the outcomes of SMT cheching
	/// </summary>
	public class SmtAttributeCheckItem
	{
		public bool Fail = false;
		public string ShortText = "";
		public string LongText = "";
	}

	/// <summary>
	/// Holds the possible attributes for an SMT specification per element
	/// as a whole.
	/// </summary>
	public class SmtAttributeRecord : ExtensionRecordBase, IExtensionSelfDescription
	{
		// self description
		public string GetSelfName() => "smt-attrtibute-set";
		public string GetSelfUri() => "https://admin-shell.io/SubmodelTemplates/smt-attribute-set/v1/0";
		public ExtensionRecordRenderInfo GetRenderInfo() => new ExtensionRecordRenderInfo()
		{
			// see https://colors.muz.li/palette/0028CD/004190/2915cd/00cd90/009064
			DisplayName = "SMT attributes",
			Abbreviation = "SMT",
			Foreground = 0xff009064,
			Background = 0xff00cd90
		};

		// attributes

		[ExtensionHintAttribute("Specifies, how many SubmodelElement instances of this " +
			"SMT element are allowed in the actual collection (hierarchy level of the Submodel).")]
		public AasSmtQualifiers.SmtCardinality Cardinality { get; set; } = AasSmtQualifiers.SmtCardinality.One;

		[ExtensionHintAttribute("Specifies an id of an equivalence class. " +
			"Only ids in the range[A-Za-z0-9] are allowed.  If multiple SMT elements feature the same equivalency " +
			"class,  only one of these are allowed in the actual collection (hierarchy level of the Submodel). ")]
		public string EitherOr { get; set; } = "";

		[ExtensionHintAttribute("Specifies the initial value of the SubmodelElement instance, when it is created " +
			"for the first time.")]
		public string InitialValue { get; set; } = "";

		[ExtensionHintAttribute("Specifies the default value of the SubmodelElement instance. " +
			"Often, this might designate a neutral, zero or empty value " +
			"depending on the valueType of a SMT element.")]
		public string DefaultValue { get; set; } = "";

		[ExtensionHintAttribute("Specifies an example value of the SubmodelElement instance, in " +
			"order to allow the user to better understand the intention nd possible values of a " +
			"SubmodelElement instance.")]
		public string ExampleValue { get; set; } = "";

		[ExtensionHintAttribute("Multiple ranges can be given by delimiting  them by '|'. " +
			"A single range is defined by interval start and  end,  either including or " +
			"excluding the given number. Interval start and end are delimited by '(', ')' resp. '[', ']'.  " +
			"The decimal point is '.'.  '*' allows to enter the default value.")]
		public string AllowedRange { get; set; } = "";

		[ExtensionHintAttribute("Specifies a regular expression validating the idShort of the created " +
			"SubmodelElement instance. The format shall conform to POSIX extended regular expressions.")]
		public string AllowedIdShort { get; set; } = "";

		[ExtensionHintAttribute("Specifies a regular expression validating the value of the created " +
			"SubmodelElement instance in its string representation. The format shall conform to POSIX " +
			"extended regular expressions.")]
		public string AllowedValue { get; set; } = "";

		[ExtensionHintAttribute("If the SMT element is a multi language property (MLP), " +
			"specifies the required languages, which shall be given.  Multiple languages can " +
			"be given by multiple Qualifiers.  Multiple languages can be given by delimiting them by '|' . " +
			"Languages are specified either by ISO 639-1 or ISO 639-2 codes.")]
		public string RequiredLang { get; set; } = "";

		[ExtensionHintAttribute("Specifies the user access mode for SubmodelElement instance. " +
			"When a Submodel is received from another party, if set to Read/Only, then the user " +
			"shall not change the value.")]
		public AasSmtQualifiers.AccessMode AccessMode { get; set; } = AasSmtQualifiers.AccessMode.ReadWrite;

		//
		// Check
		//

		/// <summary>
		/// Check a single string value
		/// </summary>
		public List<SmtAttributeCheckItem> PerformAttributeCheck(string idShort, string value,
			List<SmtAttributeCheckItem> inList = null)
		{
			// access
			var res = inList ?? new List<SmtAttributeCheckItem>();

			// be safe
			try
			{ 
				// idShort?
				if (idShort != null)
				{
					if (AllowedIdShort?.HasContent() == true)
					{
						var match = Regex.Match(idShort, AllowedIdShort);
						if (!match.Success)
							res.Add(new SmtAttributeCheckItem()
							{
								Fail = true,
								ShortText = "IdShort",
								LongText = $"Fail when checking IdShort {idShort} vs. AllowedIdShort {AllowedIdShort}."
							});
					}
				}

				// for the value, do not allow to disable a required matched by 
				// simply having null
				value = "" + value;
				var value0 = value.HasContent() ? value : "0";

				// AllowedRange
				if (AllowedRange?.HasContent() == true
					&& double.TryParse(value0, NumberStyles.Number, CultureInfo.InvariantCulture, out double valueDbl))
				{
					var ar = AllowedRange.Replace("*", DefaultValue);

					var inRange = false;
					foreach (var rngPart in ar.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					{
						// interval
						var match = Regex.Match(rngPart, @"(\(|\[)([0-9.-]+)\s*,\s*([0-9.-]+)(\)|\])");
						if (match.Success
							&& double.TryParse(match.Groups[2].ToString(), out double ivMin)
							&& double.TryParse(match.Groups[3].ToString(), out double ivMax))
						{
							var greaterMin = (match.Groups[1].ToString() == "(")
									? (valueDbl > ivMin)
									: (valueDbl >= ivMin);

							var lesserMax = (match.Groups[4].ToString() == ")")
									? (valueDbl < ivMax)
									: (valueDbl <= ivMax);

							if (greaterMin && lesserMax)
								inRange = true;
						}
						else
						{
							// single value?
							if (double.TryParse(rngPart, out double soloVal)
								&& soloVal == valueDbl)
								inRange = true;
						}
					}

					// okay, now check
					if (!inRange)
						res.Add(new SmtAttributeCheckItem()
						{
							Fail = true,
							ShortText = "Range",
							LongText = $"Fail when checking Value {value} vs. AllowedRange {AllowedRange}."
						});
				}

				// AllowedValue
				if (AllowedValue?.HasContent() == true)
				{
					var match = Regex.Match(value, AllowedValue);
					if (!match.Success)
						res.Add(new SmtAttributeCheckItem()
						{
							Fail = true,
							ShortText = "AllowedValue",
							LongText = $"Fail when checking Value {value} vs. AllowedValue {AllowedValue}."
						});
				}
			
			} catch (Exception ex)
			{
				LogInternally.That.CompletelyIgnoredError(ex);
				res.Add(new SmtAttributeCheckItem()
				{
					Fail = true,
					ShortText = "SMT-Attributes",
					LongText = $"Fail in given SMT-attributes when checking: " + ex.Message
				});
			}

			// okay
			return res;
		}

		/// <summary>
		/// Check a multi language element
		/// </summary>
		public List<SmtAttributeCheckItem> PerformAttributeCheck(Aas.IMultiLanguageProperty mlp,
			List<SmtAttributeCheckItem> inList = null)
		{
			// access
			if (mlp?.Value == null || mlp.Value.Count < 1)
				return inList;
			var res = inList ?? new List<SmtAttributeCheckItem>();

			// over single languages to test the Text
			for (int vi = 0; vi < mlp.Value.Count; vi++)
			{
				// element
				var mvi = mlp.Value[vi];
				if (mvi == null)
					continue;
			
				// test for value
				res = PerformAttributeCheck(
					idShort: (vi == 0) ? mlp.IdShort : null,
					value: mvi.Text,
					inList: res);
			}

			// over required langauge, to test if actual languages are there!
			if (RequiredLang?.HasContent() == true)
			{
				// over all required languages
				var lngMissing = new List<string>();
				foreach (var rlng in RequiredLang.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					var found = false;
					foreach (var mvi in mlp.Value)
						if (mvi.Language?.Trim() == rlng)
							found = true;

					if (!found)
						lngMissing.Add(rlng);
				}

				// okay, now check
				if (lngMissing.Count > 0)
					res.Add(new SmtAttributeCheckItem()
					{
						Fail = true,
						ShortText = "RequiredLang",
						LongText = $"Fail when checking RequiredLang {RequiredLang}. " +
							$"Missing languages are: {string.Join("|", lngMissing)}."
					});
			}

			// okay
			return res;
		}

		/// <summary>
		/// Check a collection of elements (for cardinality).
		/// Note: this function needs a lambda for looking up SMT attribute records
		///       of subordinate elements either by Referable or SemanticId reference.
		/// </summary>
		public static List<SmtAttributeCheckItem> PerformAttributeCheck(List<Aas.ISubmodelElement> elems,
			Func<Aas.IReferable, SmtAttributeRecord> lambdaLookupSmtRec,
			List<SmtAttributeCheckItem> inList = null)
		{
			// access
			if (elems == null || elems.Count < 1)
				return inList;
			var res = inList ?? new List<SmtAttributeCheckItem>();

			// make two dictionaries on these elements
			// (to count elemens per semantic id and have SmtAttributes available)
			var elemPerSemId = new MultiValueDictionary<string, Aas.ISubmodelElement>();
			var attrRecPerSemId = new MultiValueDictionary<string, SmtAttributeRecord>();
			foreach (var elem in elems)
				if (elem.SemanticId?.IsValid() == true)
				{
					// 1st
					var ssi = elem.SemanticId.ToStringExtended();
					elemPerSemId.Add(ssi, elem);

					// 2nd
					var smtr = lambdaLookupSmtRec?.Invoke(elem);
					if (smtr != null)
						attrRecPerSemId.Add(ssi, smtr);
				}

			// now can use the key of one dictionary to check it values by
			// looking up the contraints within the seconde
			foreach (var ssiKey in elemPerSemId.Keys)
			{
				// access
				var els = elemPerSemId.All(ssiKey).ToList();
				var rec = attrRecPerSemId.All(ssiKey).FirstOrDefault();
				if (rec == null)
					continue;

				// check
				var complain = "";
				if (rec.Cardinality == SmtCardinality.One && els.Count != 1)
					complain = "[1]";
				if (rec.Cardinality == SmtCardinality.OneToMany && els.Count < 1)
					complain = "[1..*]";
				if (rec.Cardinality == SmtCardinality.ZeroToOne && els.Count > 1)
					complain = "[0..1]";

				// give out
				if (complain.HasContent())
					res.Add(new SmtAttributeCheckItem()
						{
							Fail = true,
							ShortText = "Cardinality",
							LongText = $"Fail when checking Cardinality on dependent elements for semanticId {ssiKey}: " +
								$"Required {complain} but found {0 + els.Count}."
						});
			}

			// okay
			return res;
		}
	}

	public class ExtensionRecords
	{
		public static Dictionary<string, ExtensionRecordBase> AllRecords =
			new Dictionary<string, ExtensionRecordBase>();

		static ExtensionRecords()
		{
			Action<ExtensionRecordBase> add = (sme) =>
			{
				if (sme is IExtensionSelfDescription sd)
					AllRecords.Add(sd.GetSelfUri(), sme);
			};

			add(new SmtAttributeRecord());
		}

		public static ExtensionRecordBase GetTypeInstFromUri(string uri)
		{
			foreach (var rec in AllRecords.Values)
				if (rec is IExtensionSelfDescription ssd && ssd.GetSelfUri() == uri)
					return rec;
			return null;
		}
	}
}
