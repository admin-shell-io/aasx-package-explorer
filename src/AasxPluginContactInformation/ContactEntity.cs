/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using AasxIntegrationBase;
using AasxIntegrationBaseGdi;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace AasxPluginContactInformation
{
    /// <summary>
    /// Representation of a VDI2770 Document or so ..
    /// </summary>
    public class ContactEntity
    {
        public delegate void ContactEntityEvent(ContactEntity e);
        public event ContactEntityEvent DoubleClick = null;

        public delegate Task MenuClickDelegate(ContactEntity e, string menuItemHeader, object tag);
        public event MenuClickDelegate MenuClick = null;

        // parsed information

        public enum SubmodelVersion { Default = 0, V10 = 1, V11 = 2 }

        public SubmodelVersion SmVersion = SubmodelVersion.Default;

        public string Headline = "";
        public string Role = "";
        public List<string> BodyItems = new List<string>();
        public string CountryCode = "";
        public Aas.IResource PreviewFile;

		// help info

		public List<Aas.ISubmodelElement> SourceElementsContact = null;

		public AnyUiImage ImgContainerAnyUi = null;

		/// <summary>
		/// The parsing might add a dedicated, version-specific action to add.
		/// </summary>        
		public delegate bool AddPreviewFileDelegate(ContactEntity e, string path, string contentType);
        public AddPreviewFileDelegate AddPreviewFile;
      
        public ContactEntity() { }

        public void RaiseDoubleClick()
        {
            if (DoubleClick != null)
                DoubleClick(this);
        }

        public async Task RaiseMenuClick(string menuItemHeader, object tag)
        {
            await MenuClick?.Invoke(this, menuItemHeader, tag);
        }

        /// <summary>
        /// This function needs to be called as part of tick-Thread in STA / UI thread
        /// </summary>
        public AnyUiBitmapInfo LoadImageFromPath(string fn)
        {
            // be a bit suspicous ..
            if (!System.IO.File.Exists(fn))
                return null;

            // convert here, as the tick-Thread in STA / UI thread
            try
            {
                ImgContainerAnyUi.BitmapInfo = AnyUiGdiHelper.CreateAnyUiBitmapInfo(fn);
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }
            return null;
        }

		/// <summary>
		/// This function needs to be called as part of tick-Thread in STA / UI thread
		/// </summary>
		public AnyUiBitmapInfo LoadImageFromResource(string path)
		{
			// convert here, as the tick-Thread in STA / UI thread
			try
			{
				ImgContainerAnyUi.BitmapInfo = AnyUiGdiHelper.CreateAnyUiBitmapFromResource(path,
                    assembly: Assembly.GetExecutingAssembly());
			}
			catch (Exception ex)
			{
				LogInternally.That.SilentlyIgnoredError(ex);
			}
			return null;
		}
	}

    public class ListOfContactEntity : List<ContactEntity>
    {
        //
        // Default
        //

        public static ListOfContactEntity ParseSubmodelForV10(
            AdminShellPackageEnv thePackage,
            Aas.Submodel subModel, AasxPluginContactInformation.ContactInformationOptions options,
            string defaultLang,
            int selectedDocClass, AasxLanguageHelper.LangEnum selectedLanguage)
        {
            // set a new list
            var its = new ListOfContactEntity();

            // get semantic defs
            var defs = AasxPredefinedConcepts.IdtaContactInformationV10.Static;
            var mm = MatchMode.Relaxed;

            // look for Documents
            if (subModel?.SubmodelElements != null)
                foreach (var smcContact in
                    subModel.SubmodelElements.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                        defs.CD_ContactInformation, mm))
                {
                    // access
                    if (smcContact?.Value == null)
                        continue;
                    
                    // create entity, remember source
                    var ent = new ContactEntity();
                    ent.SourceElementsContact = smcContact?.Value;

                    // personal names?
                    var personNameTaken = false;
                    var lastName = 
                        "" + smcContact.Value.FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
                            defs.CD_NameOfContact, mm)?.Value?.GetDefaultString(defaultLang);

                    var middleNames =
                        "" + string.Join(" ",
                            smcContact.Value.FindAllSemanticIdAs<Aas.IMultiLanguageProperty>(
                                defs.CD_MiddleNames, mm)
                            .Select((mlp) => mlp.Value?.GetDefaultString(defaultLang)));

					var firstName =
						  "" + smcContact.Value.FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
							  defs.CD_FirstName, mm)?.Value?.GetDefaultString(defaultLang);

					var acadTitle =
						  "" + smcContact.Value.FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
							  defs.CD_AcademicTitle, mm)?.Value?.GetDefaultString(defaultLang);

                    var personName = "";
					if (lastName.HasContent())
                    {
						personName = lastName.ToUpper();
                        if (firstName.HasContent() || middleNames.HasContent())
							personName += ", " + firstName + " " + middleNames;
                        if (acadTitle.HasContent())
							personName += ", " + acadTitle;
                    }

					if (personName.HasContent())
					{
						personNameTaken = true;
                        ent.Headline = personName; 
					}

					// department?
					var departmentTaken = false;
					var department =
						"" + smcContact.Value.FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
							defs.CD_Department, mm)?.Value?.GetDefaultString(defaultLang);
                    if (!personNameTaken && department.HasContent())
                    {
                        departmentTaken = true;
                        ent.Headline = department;
					}

					// company?
					var companyTaken = false;
					var company =
						"" + smcContact.Value.FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
							defs.CD_Company, mm)?.Value?.GetDefaultString(defaultLang);
					if (!personNameTaken && !departmentTaken && company.HasContent())
					{
						companyTaken = true;
						ent.Headline = company;
					}

                    // any headline?
                    if (!ent.Headline.HasContent())
                        ent.Headline = "???";

					// cole?
					var role =
						"" + smcContact.Value.FindFirstSemanticIdAs<Aas.Property>(
							defs.CD_RoleOfContactPerson, mm)?.Value;
                    if (role.HasContent())
                        ent.Role = role;
                    else
                        // put something visual
                        ent.Role = "";

                    // country code?
                    ent.CountryCode =
						"" + smcContact.Value.FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(
							defs.CD_NationalCode, mm)?.Value?.GetDefaultString(defaultLang);

					// start body items
					ent.BodyItems = new List<string>();

                    if (!personNameTaken && personName.HasContent())
                        ent.BodyItems.Add(personName);
					if (!departmentTaken && department.HasContent())
						ent.BodyItems.Add(department);
					if (!companyTaken && company.HasContent())
						ent.BodyItems.Add(company);

                    // more body items

					Action<List<Aas.ISubmodelElement>, string, Aas.IKey> tryAdd = (coll, header, key) =>
					{
						var st = coll?.FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(key, mm)?
							        .Value?.GetDefaultString(defaultLang);
                        st = st ?? coll?.FindFirstSemanticIdAs<Aas.IProperty>(key, mm)?.Value;
						if (st?.HasContent() == true)
                            ent.BodyItems.Add(st);
					};

					tryAdd(smcContact.Value, null, defs.CD_ZipCodeOfPOBox?.GetSingleKey());
					tryAdd(smcContact.Value, null, defs.CD_POBox?.GetSingleKey());
					tryAdd(smcContact.Value, null, defs.CD_Street?.GetSingleKey());
					tryAdd(smcContact.Value, null, defs.CD_CityTown?.GetSingleKey());
					tryAdd(smcContact.Value, null, defs.CD_StateCounty?.GetSingleKey());
					tryAdd(smcContact.Value, null, defs.CD_NationalCode?.GetSingleKey());
					tryAdd(smcContact.Value, null, defs.CD_FurtherDetailsOfContact?.GetSingleKey());
					tryAdd(smcContact.Value, null, defs.CD_AddressOfAdditionalLink?.GetSingleKey());

					// even more in sub-entities?

					// Phone

					var smc2 = smcContact.Value?
						.FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Phone?.GetSingleKey(), mm);
					if (smc2 != null)
						tryAdd(smc2.Value, null, defs.CD_TelephoneNumber?.GetSingleKey());

					// Fax

					smc2 = smcContact.Value?
						.FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(
                            defs.CD_Fax?.GetSingleKey(), mm);
					if (smc2 != null)
						tryAdd(smc2.Value, null, defs.CD_FaxNumber?.GetSingleKey());

					// Email

					smc2 = smcContact.Value?
						.FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(
                            defs.CD_Email?.GetSingleKey(), mm);
					if (smc2 != null)
						tryAdd(smc2.Value, null, defs.CD_EmailAddress?.GetSingleKey());

					// IP communication
					var smc3 = smcContact.Value?
						.FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(
                            defs.CD_IPCommunication?.GetSingleKey(), mm);
					if (smc3 != null)
						tryAdd(smc3.Value, null, defs.CD_AddressOfAdditionalLink?.GetSingleKey());

					// preview?
					var fl = smcContact.Value.FindFirstSemanticIdAs<Aas.File>(
							AasxPredefinedConcepts.SmtAdditions.Static.CD_ContactInfoPreviewFile, mm);
                    if (fl != null)
                        ent.PreviewFile = new Aas.Resource(fl.Value, fl.ContentType);

                    // add
                    its.Add(ent);
				}

            // ok
            return its;
        }

    }
}
