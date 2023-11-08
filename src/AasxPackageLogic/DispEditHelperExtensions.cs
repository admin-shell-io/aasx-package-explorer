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
using VDS.RDF.Query.Algebra;
using Microsoft.VisualBasic.ApplicationServices;
using static Lucene.Net.Queries.Function.ValueSources.MultiFunction;
using System.Windows.Controls;
using System.DirectoryServices;
using AngleSharp.Dom;
using Aml.Engine.CAEX;

namespace AasxPackageLogic
{
	/// <summary>
	/// This class extends the AAS meta model editing function for those related to
	/// SAMM (Semantic Aspect Meta Model) elements. 
	/// </summary>
	public class DispEditHelperExtensions : DispEditHelperModules
	{
		public static void SammExtensionHelperUpdateJson(Aas.IExtension se, Type smtType, SmtModelElement smtInst)
		{
			// trivial
			if (se == null || smtType == null || smtInst == null)
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
				json = JsonConvert.SerializeObject(smtInst, smtType, settings);
			}
			catch (Exception ex)
			{
				LogInternally.That.SilentlyIgnoredError(ex);
			}

			// save this to the extension
			se.Value = json;
			se.ValueType = DataTypeDefXsd.String;
		}

		/// <summary>
		/// Shall provide rather quick access to information ..
		/// </summary>
		public static SmtModelElement CheckReferableForSmtExtensionType(Aas.IReferable rf)
		{
			// access
			if (rf?.Extensions == null)
				return null;

			// find any?
			foreach (var se in rf.Extensions)
				if (se.SemanticId?.IsValid() == true && se.SemanticId.Keys.Count == 1)
				{
					var t = SmtModelElements.GetTypeInstFromUri(se.SemanticId.Keys[0].Value);
					if (t != null)
						return t;
				}

			// no?
			return null;
		}

		public static IEnumerable<SmtModelElement> CheckReferableForSammElements(Aas.IReferable rf)
		{
			// access
			if (rf?.Extensions == null)
				yield break;

			// find any?
			foreach (var se in rf.Extensions)
				if (se.SemanticId?.IsValid() == true && se.SemanticId.Keys.Count == 1)
				{
					// get type 
					var smtTypeInst = SmtModelElements.GetTypeInstFromUri(se.SemanticId.Keys[0].Value);
					if (smtTypeInst == null)
						continue;

					// get instance data
					SmtModelElement sammInst = null;

					// try to de-serializa extension value
					try
					{
						if (se.Value != null)
							sammInst = JsonConvert.DeserializeObject(se.Value, smtTypeInst.GetType()) as SmtModelElement;
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

		public void DisplayOrEditEntitySmtExtensions(
			Aas.Environment env, 
			AnyUiStackPanel stack,
			List<Aas.IExtension> smtExtension,
			Action<List<Aas.IExtension>> setOutput,
			string[] addPresetNames = null, List<Aas.IKey>[] addPresetKeyLists = null,
			Aas.IReferable relatedReferable = null,
			AasxMenu superMenu = null)
		{
			// access
			if (stack == null)
				return;

			// members
			this.AddGroup(stack, "SMT extensions \u00ab experimental \u00bb :", levelColors.MainSection);

			this.AddHintBubble(
				stack, hintMode,
				new[] {
					new HintCheck(
						() => { return smtExtension == null ||
							smtExtension.Count < 1; },
						"For modelling Submodel template specifications (SMT), a set of particular attributes " +
						"to the elements of SMTs are specified. These attributes can be added as specific " +
						"Qualifiers or via adding an extension as a whole.",
						breakIfTrue: true, severityLevel: HintCheck.Severity.Notice),
					new HintCheck(
						() => { return smtExtension.Where(p => Samm.Util.HasSammSemanticId(p)).Count() > 1; },
						"Only one SMT extension is allowed per element.",
						breakIfTrue: true),
				});
			if (this.SafeguardAccess(
					stack, this.repo, smtExtension, "SMT extensions:", "Create data element!",
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
						stack, "Spec. records:", repo: repo,
						superMenu: superMenu,
						ticketMenu: new AasxMenu()
							.AddAction("add-smt-attributes", "Add attribute set",
								"Add the attribute set as a whole.")
							.AddAction("delete-last", "Delete last extension",
								"Deletes last extension."),
						ticketAction: (buttonNdx, ticket) =>
						{
							if (buttonNdx == 0)
							{							
								// new
								var newSet = new SmtAttributeSet();

								// now add
								smtExtension.Add(
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
								if (smtExtension.Count > 0)
									smtExtension.RemoveAt(smtExtension.Count - 1);
								else
									setOutput?.Invoke(null);
							}

							this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
							return new AnyUiLambdaActionRedrawEntity();
						});
				}

				// now use the normal mechanism to deal with editMode or not ..
				if (smtExtension != null && smtExtension.Count > 0)
				{
					var numSammExt = 0;

					for (int i = 0; i < smtExtension.Count; i++)
					{
						// get type 
						var se = smtExtension[i];
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
	/// Shall be implemented by all non-abstract model elements
	/// </summary>
	public interface ISmtSelfDescription
	{
		/// <summary>
		/// get short name, which can als be used to distinguish elements.
		/// </summary>
		string GetSelfName();

		/// <summary>
		/// Get URN of this element class.
		/// </summary>
		string GetSelfUri();
	}

	/// <summary>
	/// This class provides generators for Qualifiers, Extension etc.
	/// in order to express preset-based information e.g. Cardinality
	/// </summary>
	public static class AasPresetHelper
	{
		/// <summary>
		/// Semantically different, but factually equal to <c>FormMultiplicity</c>
		/// </summary>
		public enum SmtCardinality { ZeroToOne = 0, One, ZeroToMany, OneToMany };

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
	}

	/// <summary>
	/// Abstract base class for information for Submodel template specifications.
	/// </summary>
	public class SmtModelElement
	{		
	}

	/// <summary>
	/// Holds the possible attributes for an SMT specification per element
	/// as a whole.
	/// </summary>
	public class SmtAttributeSet : SmtModelElement, ISmtSelfDescription
	{
		// self description
		public string GetSelfName() => "smt-attrtibute-set";
		public string GetSelfUri() => "https://admin-shell.io/SubmodelTemplates/smt-attribute-set/v1/0";

		// attributes

		/// <summary>
		/// This Qualifier allows to specify, how many SubmodelElement 
		/// instances of this SMT element are allowed in the actual 
		/// collection (hierarchy level of the Submodel).
		/// </summary>
		public AasPresetHelper.SmtCardinality Cardinality { get; set; } = AasPresetHelper.SmtCardinality.One;

		/// <summary>
		/// The Qualifier.value defines an id of an equivalence class. 
		/// Only ids in the range[A-Za-z0-9] are allowed. 
		/// If multiple SMT elements feature the same equivalence class, 
		/// only one of these are allowed in the actual collection
		/// (hierarchy level of the Submodel). 
		/// </summary>
		public string EitherOr { get; set; } = "";

		/// <summary>
		/// Specifies the initial value of the SubmodelElement instance, 
		/// when it is created for the first time.
		/// </summary>
		public string InitialValue { get; set; } = "";

		/// <summary>
		/// Specifies the default value of the SubmodelElement instance.
		/// Often, this might designate a neutral, zero or empty value 
		/// depending on the valueType of a SMT element.
		/// </summary>
		public string DefaultValue { get; set; } = "";

		/// <summary>
		/// Specifies an example value of the SubmodelElement instance, 
		/// in order to allow the user to better understand the intention 
		/// and possible values of a SubmodelElement instance.
		/// </summary>
		public string ExampleValue { get; set; } = "";

		/// <summary>
		/// Specifies a set of allowed continous numerical ranges. 
		/// Multiple ranges can be given by delimiting  them by '|'. 
		/// A single range is defined by interval start and  end, 
		/// either including or excluding the given number. 
		/// Interval start and end are delimited by ','; 
		/// '.' is  the decimal point. 
		/// '*' allows to enter the default value
		/// </summary>
		public string AllowedRange { get; set; } = "";

		/// <summary>
		/// Specifies a regular expression validating the idShort of the 
		/// created SubmodelElement instance. The format shall 
		/// conform to POSIX extended regular expressions.
		/// </summary>
		public string AllowedIdShort { get; set; } = "";

		/// <summary>
		/// Specifies a regular expression validating the value of the created 
		/// SubmodelElement instance in its string representation. The format 
		/// shall conform to POSIX extended regular expressions.
		/// </summary>
		public string AllowedValue { get; set; } = "";

		/// <summary>
		/// If the SMT element is a multi language property (MLP), 
		/// specifies the required languages, which shall be given. 
		/// Multiple languages can be given by multiple Qualifiers. 
		/// Multiple languages can be given by delimiting them by '|' .
		/// Languages are specified either by ISO 639-1 or ISO 639-2 codes.
		/// </summary>
		public string RequiredLang { get; set; } = "";

		/// <summary>
		/// Specifies the user access mode for SubmodelElement instance.
		/// When a Submodel is received from another party, if set to 
		/// Read/Only, then the user shall not change the value.
		/// </summary>
		public string AccessMode { get; set; } = "";
	}

	public class SmtModelElements
	{
		public static Dictionary<string, SmtModelElement> AllElements =
			new Dictionary<string, SmtModelElement>();

		static SmtModelElements()
		{
			Action<SmtModelElement> add = (sme) =>
			{
				if (sme is ISmtSelfDescription sd)
					AllElements.Add(sd.GetSelfUri(), sme);
			};

			add(new SmtAttributeSet());
		}

		public static SmtModelElement GetTypeInstFromUri(string uri)
		{
			foreach (var x in AllElements.Values)
				if (x is ISmtSelfDescription ssd && ssd.GetSelfUri() == uri)
					return x;
			return null;
		}
	}

}
