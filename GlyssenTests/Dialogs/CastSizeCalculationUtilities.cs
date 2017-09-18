﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Glyssen;
using Glyssen.Bundle;
using Glyssen.Character;
using Glyssen.Dialogs;
using Glyssen.Rules;
using NUnit.Framework;
using SIL.Scripture;
using SIL.WritingSystems;

namespace GlyssenTests.Dialogs
{
	/// <summary>
	/// This class and the one below are designed to help the developer calculate and set the minimum
	/// cast sizes needed for each book in the Bible. For the test to run properly, you will need
	/// projects set up at
	/// C:\ProgramData\FCBH-SIL\Glyssen\ach\3b9fdc679b9319c3\Acholi New Test 1985 Audio\ach.glyssen
	/// and
	/// C:\ProgramData\FCBH-SIL\Glyssen\cuk\5a6b88fafe1c8f2b\The Bible in Kuna, San Blas Audio (1)\cuk.glyssen
	///
	/// There are instructions pumped out to the unit test results console telling you what to do with the results.
	///
	/// AFAIK, test failures should not be considered problems with the code but rather problems with the data.
	/// </summary>
	[Category("ByHand")]
	[TestFixture]
	class CalculateMinimumCastSizesForNewTestamentBasedOnAcholi
	{
		private Project m_project;
		private readonly ConcurrentDictionary<string, CastSizeRowValues> m_results = new ConcurrentDictionary<string, CastSizeRowValues>();

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			// Use the real version of the file because we want the results to be based on the production control file.
			ControlCharacterVerseData.TabDelimitedCharacterVerseData = null;
			CharacterDetailData.TabDelimitedCharacterDetailData = null;

			Sldr.Initialize();
			try
			{
				m_project =
					Project.Load(@"C:\ProgramData\FCBH-SIL\Glyssen\ach\3b9fdc679b9319c3\Acholi New Test 1985 Audio\ach.glyssen");
				TestProject.SimulateDisambiguationForAllBooks(m_project);
				m_project.CharacterGroupGenerationPreferences.NarratorsOption = NarratorsOption.SingleNarrator;
			}
			catch
			{
				// If we have an exception here, TestFixtureTearDown doesn't get called which means we need to call Sldr.Cleanup() now
				Sldr.Cleanup();
				throw;
			}
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			Sldr.Cleanup();
			var ntBooks = SilBooks.Codes_3Letter.Skip(39).ToArray();

			if (m_results.Count == 27 || m_results.Count == 1)
			{
				foreach (var bookCode in ntBooks)
				{
					CastSizeRowValues validCast;
					if (m_results.TryGetValue(bookCode, out validCast))
						Debug.WriteLine("[TestCase(\"" + bookCode + "\", " + (validCast.Male - 1) + ")]");
				}
				Debug.WriteLine("****************");
			}
			else
			{
				Debug.WriteLine("WARNING: not all NT books are included in these results!!!!!!!!!!!");
			}

			Debug.WriteLine("Copy and paste the following into the CastSizePlanningViewModel constructor:");
			Debug.WriteLine("");

			foreach (var bookCode in ntBooks)
			{
				CastSizeRowValues validCast;
				if (m_results.TryGetValue(bookCode, out validCast))
				{
					Debug.WriteLine("case \"" + bookCode + "\":");
					if (bookCode == "HEB")
					{
						Debug.WriteLine("switch (m_project.DramatizationPreferences.ScriptureQuotationsShouldBeSpokenBy)");
						Debug.WriteLine("{");
						Debug.WriteLine("\tcase DramatizationOption.DedicatedCharacter:");
						Debug.WriteLine("\t\tsmallCast.Male = Math.Max(smallCast.Male, 1);");
						Debug.WriteLine("\t\tbreak;");
						Debug.WriteLine("\tcase DramatizationOption.DefaultCharacter:");
						Debug.WriteLine("\t\tsmallCast.Male = Math.Max(smallCast.Male, 4);");
						Debug.WriteLine("\t\tbreak;");
						Debug.WriteLine("\tcase DramatizationOption.Narrator:");
						Debug.WriteLine("\t\tsmallCast.Male = Math.Max(smallCast.Male, 0);");
						Debug.WriteLine("\t\tbreak;");
						Debug.WriteLine("}");
					}
					else
						Debug.WriteLine("smallCast.Male = Math.Max(smallCast.Male, " + (validCast.Male - 2) + ");");
					if (validCast.Female != 2)
						Debug.WriteLine("smallCast.Female = " + validCast.Female + ";");
					Debug.WriteLine("break;");
				}
			}
		}

		[Category("ByHand")]
		[TestCase("MAT", 11)]
		[TestCase("MRK", 13)]
		[TestCase("LUK", 14)]
		[TestCase("JHN", 11)]
		[TestCase("ACT", 14)]
		[TestCase("ROM", 3)]
		[TestCase("1CO", 6)]
		[TestCase("2CO", 4)]
		[TestCase("GAL", 4)]
		[TestCase("EPH", 1)]
		[TestCase("PHP", 1)]
		[TestCase("COL", 2)]
		[TestCase("1TH", 1)]
		[TestCase("2TH", 1)]
		[TestCase("1TI", 1)]
		[TestCase("2TI", 1)]
		[TestCase("TIT", 2)]
		[TestCase("PHM", 1)]
		[TestCase("HEB", 2)]
		[TestCase("JAS", 3)]
		[TestCase("1PE", 1)]
		[TestCase("2PE", 3)]
		[TestCase("1JN", 1)]
		[TestCase("2JN", 1)]
		[TestCase("3JN", 1)]
		[TestCase("JUD", 4)]
		[TestCase("REV", 16)]
		public void UtilityToCalculateMinimumCastSizesForAcholi_ThisIsNotARealUnitTest(string bookCode, int initialGuess)
		{
			foreach (var book in m_project.AvailableBooks)
			{
				book.IncludeInScript = book.Code == bookCode;
			}
			m_project.ClearCharacterStatistics();

			CastSizeRowValues validCast = new CastSizeRowValues(initialGuess + 2, 2, 1);
			var currentCast = new CastSizeRowValues(initialGuess + 1, 2, 1);
			List<CharacterGroup> groups;
			do
			{
				var gen = new CharacterGroupGenerator(m_project, currentCast);

				groups = gen.GenerateCharacterGroups(true);
				if (groups != null)
					validCast.Male = currentCast.Male;
				currentCast.Male--;

			} while (groups != null && currentCast.Male >= 2);
			Assert.IsTrue(validCast.Male <= initialGuess + 1);
			m_results[bookCode] = validCast;
		}
	}

	[Category("ByHand")]
	[TestFixture]
	class CalculateMinimumCastSizesForOldTestamentBasedOnKunaSanBlas
	{
		private Project m_project;
		private readonly ConcurrentDictionary<string, CastSizeRowValues> m_results = new ConcurrentDictionary<string, CastSizeRowValues>();

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			// Use the real version of the file because we want the results to be based on the production control file.
			ControlCharacterVerseData.TabDelimitedCharacterVerseData = null;
			CharacterDetailData.TabDelimitedCharacterDetailData = null;

			Sldr.Initialize();
			try
			{
				//Change this to Kuna and finish tests for OT books
				m_project =
					Project.Load(
						@"C:\ProgramData\FCBH-SIL\Glyssen\cuk\5a6b88fafe1c8f2b\The Bible in Kuna, San Blas Audio (1)\cuk.glyssen");
				TestProject.SimulateDisambiguationForAllBooks(m_project);
				m_project.CharacterGroupGenerationPreferences.NarratorsOption = NarratorsOption.SingleNarrator;

			}
			catch
			{
				// If we have an exception here, TestFixtureTearDown doesn't get called which means we need to call Sldr.Cleanup() now.
				// This can affect other tests, otherwise.
				Sldr.Cleanup();
				throw;
			}
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			Sldr.Cleanup();

			var otBooks = SilBooks.Codes_3Letter.Take(39).ToArray();

			if (m_results.Count == 39 || m_results.Count == 1)
			{
				foreach (var bookCode in otBooks)
				{
					CastSizeRowValues validCast;
					if (m_results.TryGetValue(bookCode, out validCast))
						Debug.WriteLine("[TestCase(\"" + bookCode + "\", " + (validCast.Male - 1) + ")]");
				}
				Debug.WriteLine("****************");
			}
			else
			{
				Debug.WriteLine("WARNING: not all OT books are included in these results!!!!!!!!!!!");
			}

			Debug.WriteLine("Copy and paste the following into the CastSizePlanningViewModel constructor:");
			Debug.WriteLine("");

			foreach (var bookCode in otBooks)
			{
				CastSizeRowValues validCast;
				if (m_results.TryGetValue(bookCode, out validCast))
				{
					Debug.WriteLine("case \"" + bookCode + "\":");
					Debug.WriteLine("smallCast.Male = Math.Max(smallCast.Male, " + (validCast.Male - 2) + ");");
					if (validCast.Female != 2)
						Debug.WriteLine("smallCast.Female = " + validCast.Female + ";");
					Debug.WriteLine("break;");
				}
			}
		}

		[Category("ByHand")]
		[TestCase("GEN", 10)]
		[TestCase("EXO", 7)]
		[TestCase("LEV", 4)]
		[TestCase("NUM", 9)]
		[TestCase("DEU", 3)]
		[TestCase("JOS", 8)]
		[TestCase("JDG", 9)]
		[TestCase("RUT", 4)]
		[TestCase("1SA", 12)]
		[TestCase("2SA", 13)]
		[TestCase("1KI", 11)]
		[TestCase("2KI", 10)]
		[TestCase("1CH", 6)]
		[TestCase("2CH", 10)]
		[TestCase("EZR", 6)]
		[TestCase("NEH", 8)]
		[TestCase("EST", 7)]
		[TestCase("JOB", 8)]
		[TestCase("PSA", 4)]
		[TestCase("PRO", 1)]
		[TestCase("ECC", 2)]
		[TestCase("SNG", 3)]
		[TestCase("ISA", 7)]
		[TestCase("JER", 10)]
		[TestCase("LAM", 2)]
		[TestCase("EZK", 5)]
		[TestCase("DAN", 6)]
		[TestCase("HOS", 3)]
		[TestCase("JOL", 3)]
		[TestCase("AMO", 5)]
		[TestCase("OBA", 2)]
		[TestCase("JON", 5)]
		[TestCase("MIC", 4)]
		[TestCase("NAM", 2)]
		[TestCase("HAB", 2)]
		[TestCase("ZEP", 2)]
		[TestCase("HAG", 3)]
		[TestCase("ZEC", 6)]
		[TestCase("MAL", 2)]
		public void UtilityToCalculateMinimumCastSizesForKuna_ThisIsNotARealUnitTest(string bookCode, int initialGuess)
		{
			if (bookCode == "SNG")
			{
				// Song of Solomon is special because all the speech is "Implicit" (which we don't handle properly yet)
				m_results[bookCode] = new CastSizeRowValues(4, 2, 0);
				return;
			}

			foreach (var book in m_project.AvailableBooks)
				book.IncludeInScript = book.Code == bookCode;
			m_project.IncludedBooks.Single().SingleVoice = false;

			m_project.ClearCharacterStatistics();

			var women = (bookCode == "RUT") ? 4 : 2;
			if (bookCode == "GEN" || bookCode == "EXO")
				women = 3;
			CastSizeRowValues validCast = new CastSizeRowValues(initialGuess + 2, women, 1);
			var currentCast = new CastSizeRowValues(initialGuess + 1, women, 1);
			List<CharacterGroup> groups = null;
			do
			{
				var gen = new CharacterGroupGenerator(m_project, currentCast);

				groups = gen.GenerateCharacterGroups(true);
				if (groups != null)
					validCast.Male = currentCast.Male;
				currentCast.Male--;

			} while (groups != null && currentCast.Male >= 2);
			Assert.IsTrue(validCast.Male <= initialGuess + 1);
			m_results[bookCode] = validCast;
		}
	}
}