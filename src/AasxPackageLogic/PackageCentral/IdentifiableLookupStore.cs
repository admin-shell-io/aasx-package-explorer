/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Aas = AasCore.Aas3_0;

namespace AasxPackageLogic.PackageCentral
{
	/// <summary>
	/// This class provides a layered access to a list (a <c>IEnumerable</c>) of
	/// <c>Aas.IIdentifiable</c>. This access could be provided by each time
	/// blindly iterating through the list or by caching the <c>Id</c>s in a
	/// dictionary. Result can be access immedeatily or selected by an
	/// lambda, so that only portions of the Identifiable are directly available 
	/// and strongly typed.
	/// </summary>
	/// <typeparam name="I">Subtype of <c>Aas.IIdentifiable</c></typeparam>
	/// <typeparam name="ME">Result type, selected by an lambda</typeparam>
	public class IdentifiableLookupStore<I, ME> where I : Aas.IIdentifiable where ME : class
	{
		protected IEnumerable<I>[] _originalData = null;

		protected Func<I, ME> _lambdaSelectResult = null;

		protected MultiValueDictionary<string, I> _lookup = null;

		protected bool IsValidForDict() =>
			_originalData != null && _lambdaSelectResult != null && _lookup != null;

		public void StartDictionaryAccess(
			IEnumerable<I>[] originalData,
			Func<I, ME> lambdaSelectResult)
		{
			// remember
			_originalData = originalData;
			_lambdaSelectResult = lambdaSelectResult;

			// create the dictionary
			_lookup = new MultiValueDictionary<string, I>();
			if (!IsValidForDict())
			{
				_lookup = null;
				return;
			}

			// populate
			foreach (var odItem in _originalData)
				foreach (var idf in odItem)
					if (idf?.Id != null)
						_lookup.Add(idf.Id, idf);
		}

		/// <summary>
		/// Lookup all elements for id <c>idKey</c> and 
		/// return the declared Identifiable subtype.
		/// </summary>
		public IEnumerable<I> LookupAllIdent(string idKey)
		{
			if (idKey == null || !IsValidForDict() || !_lookup.ContainsKey(idKey))
				yield break;

			foreach (var idf in _lookup[idKey])
				yield return idf;
		}

		/// <summary>
		/// Lookup first element for id <c>idKey</c> and 
		/// return the declared Identifiable subtype. Else: return <c>null</c>
		/// </summary>
		public I LookupFirstIdent(string idKey)
		{
			return LookupAllIdent(idKey).FirstOrDefault();
		}

		/// <summary>
		/// Lookup all elements for id <c>idKey</c> and 
		/// return the result of the given lambda selection.
		/// </summary>
		public IEnumerable<ME> LookupAllResult(string idKey)
		{
			foreach (var idf in LookupAllIdent(idKey))
			{
				var res = _lambdaSelectResult?.Invoke(idf);
				if (res != null)
					yield return res;
			}
		}

		/// <summary>
		/// Lookup first element for id <c>idKey</c> and 
		/// return the result of the given lambda selection.
		/// </summary>
		public ME LookupFirstResult(string idKey)
		{
			return LookupAllResult(idKey).FirstOrDefault();
		}

		/// <summary>
		/// Lookup first element for id <c>idKey</c> and 
		/// return the result of the given lambda selection.
		/// Note: just a shortcut to <c>LookupFirstResult()</c>
		/// </summary>
		public ME Lookup(string idKey)
		{
			return LookupFirstResult(idKey);
		}		
	}
}
