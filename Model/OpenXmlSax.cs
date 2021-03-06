﻿using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vulnerator.ViewModel;

namespace Vulnerator.Model
{
    class OpenXmlSax
    {
        #region Member Variables

        private int IavmFilterOne { get { return int.Parse(ConfigAlter.ReadSettingsFromDictionary("iavmFilterOne")); } }
        private int IavmFilterTwo { get { return int.Parse(ConfigAlter.ReadSettingsFromDictionary("iavmFilterTwo")); } }
        private int IavmFilterThree { get { return int.Parse(ConfigAlter.ReadSettingsFromDictionary("iavmFilterThree")); } }
        private int IavmFilterFour { get { return int.Parse(ConfigAlter.ReadSettingsFromDictionary("iavmFilterFour")); } }
        private bool IncludeOngoingFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbOpen")); } }
        private bool IncludeCompletedFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbCompleted")); } }
        private bool IncludeNotApplicableFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbNotApplicable")); } }
        private bool IncludeNotReviewedFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbNotReviewed")); } }
        private bool IncludeCriticalFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbCritical")); } }
        private bool IncludeHighFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbHigh")); } }
        private bool IncludeMediumFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbMedium")); } }
        private bool IncludeLowFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbLow")); } }
        private bool IncludeInformationalFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbInfo")); } }
        private bool IncludCatIFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbCatI")); } }
        private bool IncludCatIIFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbCatII")); } }
        private bool IncludCatIIIFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbCatIII")); } }
        private bool IncludCatIVFindings { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbCatIV")); } }
        private bool IsDiacapPackage { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("rbDiacap")); } }
        private bool AssetOverviewTabIsNeeded { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbAssetOverview")); } }
        private bool PoamAndRarTabsAreNeeded { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbPoamRar")); } }
        private bool DiscrepanciesTabIsNeeded { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbDiscrepancies")); } }
        private bool AcasOutputTabIsNeeded { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbAcasOutput")); } }
        private bool AcasFindingsShouldBeMerged { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbMergeAcas")); } }
        private bool CklFindingsShouldBeMerged { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbMergeCkl")); } }
        private bool XccdfFindingsShouldBeMerged { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbMergeXccdf")); } }
        private bool WasspFindingsShouldBeMerged { get { return bool.Parse(ConfigAlter.ReadSettingsFromDictionary("cbMergeWassp")); } }
        private Dictionary<string, int> sharedStringDictionary = new Dictionary<string, int>();
        private int sharedStringMaxIndex = 0;
        private string ContactOrganization = ConfigAlter.ReadSettingsFromDictionary("tbEmassOrg");
        private string ContactName = ConfigAlter.ReadSettingsFromDictionary("tbEmassName");
        private string ContactNumber = ConfigAlter.ReadSettingsFromDictionary("tbEmassNumber");
        private string ContactEmail = ConfigAlter.ReadSettingsFromDictionary("tbEmassEmail");
        private int poamRowCounterIndex = 1;
        private int assetOverviewRowCounterIndex = 1;
        private List<string> assetOverviewMergeCellReferences = new List<string>();
        private UInt32Value sheetIndex = 1;
        private string[] delimiter = new string[] { ",\r\n" };
        string doubleCarriageReturn = Environment.NewLine + Environment.NewLine;

        private OpenXmlWriter assetOverviewOpenXmlWriter;
        private OpenXmlWriter poamOpenXmlWriter;
        private OpenXmlWriter rarOpenXmlWriter;
        private OpenXmlWriter acasOutputOpenXmlWriter;
        private OpenXmlWriter discrepanciesOpenXmlWriter;

        #endregion Member Variables

        public string CreateExcelReport(string fileName, AsyncObservableCollection<MitigationItem> mitigationList)
        {
            try
            {
                using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
                    WorkbookStylesPart workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                    workbookStylesPart.Stylesheet = CreateStylesheet();
                    Workbook workbook = workbookPart.Workbook = new Workbook();
                    Sheets sheets = workbook.AppendChild<Sheets>(new Sheets());
                    StartSpreadsheets(workbookPart, sheets);

                    if (AssetOverviewTabIsNeeded)
                    {
                        WriteFindingTypeHeaderRowOne("ACAS");
                        WriteFindingTypeHeaderRowTwo("ACAS");
                        WriteAssetOverviewItems("ACAS");
                        WriteFindingTypeHeaderRowOne("CKL");
                        WriteFindingTypeHeaderRowTwo("CKL");
                        WriteAssetOverviewItems("CKL");
                        WriteFindingTypeHeaderRowOne("XCCDF");
                        WriteFindingTypeHeaderRowTwo("XCCDF");
                        WriteAssetOverviewItems("XCCDF");
                        WriteFindingTypeHeaderRowOne("WASSP");
                        WriteFindingTypeHeaderRowTwo("WASSP");
                        WriteAssetOverviewItems("WASSP");
                    }

                    if (PoamAndRarTabsAreNeeded)
                    {
                        WriteFindingsToPoamAndRar("ACAS", AcasFindingsShouldBeMerged, mitigationList);
                        WriteFindingsToPoamAndRar("CKL", CklFindingsShouldBeMerged, mitigationList);
                        WriteFindingsToPoamAndRar("XCCDF", XccdfFindingsShouldBeMerged, mitigationList);
                        WriteFindingsToPoamAndRar("WASSP", WasspFindingsShouldBeMerged, mitigationList);
                    }

                    EndSpreadsheets();
                    CreateSharedStringPart(workbookPart);
                }
                return "Excel report creation successful";
            }
            catch (Exception exception)
            { 
                WriteLog.LogWriter(exception, fileName);
                return "Excel report creation failed - see log for details";
            }
        }

        private void StartSpreadsheets(WorkbookPart workbookPart, Sheets sheets)
        {
            if (AssetOverviewTabIsNeeded)
            { StartAssetOverview(workbookPart, sheets); }
            if (PoamAndRarTabsAreNeeded)
            {
                StartPoam(workbookPart, sheets);
                StartRar(workbookPart, sheets);
            }
            if (AcasOutputTabIsNeeded)
            { StartAcasOutput(workbookPart, sheets); }
            if (DiscrepanciesTabIsNeeded)
            { StartDiscrepancies(workbookPart, sheets); }
        }

        private void WriteFindingsToPoamAndRar(string findingType, bool findingsAreMerged, AsyncObservableCollection<MitigationItem> mitigationList)
        {
            using (SQLiteCommand sqliteCommand = FindingsDatabaseActions.sqliteConnection.CreateCommand())
            {
                sqliteCommand.Parameters.Add(new SQLiteParameter("FindingType", findingType));
                sqliteCommand.CommandText = SetSqliteCommandTextForPoamAndRar(findingType, findingsAreMerged); 
                using (SQLiteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    while (sqliteDataReader.Read())
                    {
                        if (sqliteDataReader["VulnId"].ToString().Equals("Plugin"))
                        { continue; }
                        if (!FilterBySeverity(sqliteDataReader["Impact"].ToString(), sqliteDataReader["RawRisk"].ToString()))
                        { continue; }
                        if (!FilterByStatus(sqliteDataReader["Status"].ToString()))
                        { continue; }

                        WriteFindingToPoam(sqliteDataReader, mitigationList);
                        WriteFindingToRar(sqliteDataReader, mitigationList);
                    }
                }
            }
        }

        private void EndSpreadsheets()
        {
            if (AssetOverviewTabIsNeeded)
            { EndAssetOverview(); }
            if (PoamAndRarTabsAreNeeded)
            {
                EndPoam();
                EndRar();
            }
            if (AcasOutputTabIsNeeded)
            { EndAcasOutput(); }
            if (DiscrepanciesTabIsNeeded)
            { EndDiscrepancies(); }
        }

        #region Create Asset Overview

        private void StartAssetOverview(WorkbookPart workbookPart, Sheets sheets)
        {
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetIndex, Name = "Asset Overview" };
            sheetIndex++;
            sheets.Append(sheet);
            assetOverviewOpenXmlWriter = OpenXmlWriter.Create(worksheetPart);
            assetOverviewOpenXmlWriter.WriteStartElement(new Worksheet());
            WriteAssetOverviewColumns();
            assetOverviewOpenXmlWriter.WriteStartElement(new SheetData());
            WriteAssetOverviewHeaderRowOne();
        }

        private void WriteAssetOverviewColumns()
        {
            assetOverviewOpenXmlWriter.WriteStartElement(new Columns());
            assetOverviewOpenXmlWriter.WriteElement(new Column() { Min = 1U, Max = 1U, Width = 30d, CustomWidth = true });
            assetOverviewOpenXmlWriter.WriteElement(new Column() { Min = 2U, Max = 2U, Width = 30d, CustomWidth = true });
            assetOverviewOpenXmlWriter.WriteElement(new Column() { Min = 3U, Max = 3U, Width = 30d, CustomWidth = true });
            assetOverviewOpenXmlWriter.WriteElement(new Column() { Min = 4U, Max = 4U, Width = 60d, CustomWidth = true });
            assetOverviewOpenXmlWriter.WriteElement(new Column() { Min = 5U, Max = 5U, Width = 30d, CustomWidth = true });
            assetOverviewOpenXmlWriter.WriteElement(new Column() { Min = 7U, Max = 7U, Width = 15d, CustomWidth = true });
            assetOverviewOpenXmlWriter.WriteElement(new Column() { Min = 8U, Max = 8U, Width = 15d, CustomWidth = true });
            assetOverviewOpenXmlWriter.WriteElement(new Column() { Min = 9U, Max = 9U, Width = 15d, CustomWidth = true });
            assetOverviewOpenXmlWriter.WriteElement(new Column() { Min = 10U, Max = 10U, Width = 15d, CustomWidth = true });
            assetOverviewOpenXmlWriter.WriteElement(new Column() { Min = 11U, Max = 11U, Width = 15d, CustomWidth = true });
            assetOverviewOpenXmlWriter.WriteElement(new Column() { Min = 12U, Max = 12U, Width = 15d, CustomWidth = true });
            assetOverviewOpenXmlWriter.WriteEndElement();
        }

        private void WriteAssetOverviewHeaderRowOne()
        {
            assetOverviewOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(assetOverviewOpenXmlWriter, "Asset Overview", 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            assetOverviewOpenXmlWriter.WriteEndElement();
            assetOverviewRowCounterIndex++;
        }
        
        private void WriteFindingTypeHeaderRowOne(string findingType)
        {
            assetOverviewOpenXmlWriter.WriteElement(new Row());
            assetOverviewRowCounterIndex++;
            assetOverviewOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(assetOverviewOpenXmlWriter, findingType + " Asset Insight", 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 3);
            assetOverviewOpenXmlWriter.WriteEndElement();
            assetOverviewMergeCellReferences.Add("A" + assetOverviewRowCounterIndex.ToString() + ":K" + assetOverviewRowCounterIndex.ToString());
            assetOverviewRowCounterIndex++;
            
        }

        private void WriteFindingTypeHeaderRowTwo(string findingType)
        {
            assetOverviewOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(assetOverviewOpenXmlWriter, "Host Name", 17);
            WriteCellValue(assetOverviewOpenXmlWriter, "IP Address", 17);
            WriteCellValue(assetOverviewOpenXmlWriter, "Group Name", 17);
            switch (findingType)
            {
                case "ACAS":
                    {
                        WriteCellValue(assetOverviewOpenXmlWriter, "Operating System", 17);
                        break;
                    }
                default:
                    {
                        WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 17);
                        break;
                    }
            }
            WriteCellValue(assetOverviewOpenXmlWriter, "File Name", 17);
            WriteCellValue(assetOverviewOpenXmlWriter, "CAT I", 6);
            WriteCellValue(assetOverviewOpenXmlWriter, "CAT II", 7);
            WriteCellValue(assetOverviewOpenXmlWriter, "CAT III", 8);
            WriteCellValue(assetOverviewOpenXmlWriter, "CAT IV", 9);
            WriteCellValue(assetOverviewOpenXmlWriter, "Total", 17);
            switch (findingType)
            {
                case "ACAS":
                    {
                        WriteCellValue(assetOverviewOpenXmlWriter, "Credentialed", 17);
                        break;
                    }
                case "XCCDF":
                    {
                        WriteCellValue(assetOverviewOpenXmlWriter, "SCAP Score", 17);
                        break;
                    }
                default:
                    {
                        WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 17);
                        break;
                    }
            }
            assetOverviewOpenXmlWriter.WriteEndElement();
            assetOverviewRowCounterIndex++;
        }

        private void WriteAssetOverviewItems(string findingType)
        {
            List<AssetOverviewLineItem> assetList = new List<AssetOverviewLineItem>();
            using (SQLiteCommand sqliteCommand = FindingsDatabaseActions.sqliteConnection.CreateCommand())
            {
                sqliteCommand.Parameters.Add(new SQLiteParameter("FindingType", findingType));
                assetList = ObtainInitialAssetList(assetList, sqliteCommand);
                // assetList = IncrementAssetVulnerabilityCounts(assetList, sqliteCommand);
                // assetList = SetAssetCredentialedValue(assetList, sqliteCommand);
            }
            foreach (AssetOverviewLineItem asset in assetList)
            { WriteAssetOverviewRow(asset, findingType); }
        }

        private List<AssetOverviewLineItem> ObtainInitialAssetList(List<AssetOverviewLineItem> assetList, SQLiteCommand sqliteCommand)
        {
            sqliteCommand.CommandText = "SELECT AssetToReport, HostName, IpAddress, GroupName, FileName, " +
                "(SELECT OperatingSystem FROM UniqueFinding U2 NATURAL JOIN Vulnerability WHERE U1.AssetToReport = U2.AssetToReport AND VulnId = '11936') AS OperatingSystem, " +
                "(SELECT COUNT(Impact) FROM Vulnerability NATURAL JOIN UniqueFinding U2 WHERE Impact = 'Critical' AND RawRisk = '' AND U1.AssetToReport = U2.AssetToReport) AS CriticalCount, " +
                "(SELECT COUNT(Impact) FROM Vulnerability NATURAL JOIN UniqueFinding U2 WHERE Impact = 'High' AND RawRisk = '' AND U1.AssetToReport = U2.AssetToReport) AS HighCount, " +
                "(SELECT COUNT(RawRisk) FROM Vulnerability NATURAL JOIN UniqueFinding U2 WHERE RawRisk = 'I' AND U1.AssetToReport = U2.AssetToReport) AS CatICount, " +
                "(SELECT COUNT(Impact) FROM Vulnerability NATURAL JOIN UniqueFinding U2 WHERE Impact = 'Medium' AND RawRisk = '' AND U1.AssetToReport = U2.AssetToReport) AS MediumCount, " +
                "(SELECT COUNT(RawRisk) FROM Vulnerability NATURAL JOIN UniqueFinding U2 WHERE RawRisk = 'II' AND U1.AssetToReport = U2.AssetToReport) AS CatIICount, " +
                "(SELECT COUNT(Impact) FROM Vulnerability NATURAL JOIN UniqueFinding U2 WHERE Impact = 'Low' AND RawRisk = '' AND U1.AssetToReport = U2.AssetToReport) AS LowCount, " +
                "(SELECT COUNT(RawRisk) FROM Vulnerability NATURAL JOIN UniqueFinding U2 WHERE RawRisk = 'III' AND U1.AssetToReport = U2.AssetToReport) AS CatIIICount, " +
                "(SELECT COUNT(Impact) FROM Vulnerability NATURAL JOIN UniqueFinding U2 WHERE Impact = 'Informational' AND RawRisk = '' AND U1.AssetToReport = U2.AssetToReport) AS InfoCount, " +
                "(SELECT COUNT(RawRisk) FROM Vulnerability NATURAL JOIN UniqueFinding U2 WHERE RawRisk = 'IV' AND U1.AssetToReport = U2.AssetToReport) AS CatIVCount, " +
                "(SELECT COUNT(Impact) FROM Vulnerability NATURAL JOIN UniqueFinding U2 WHERE U1.AssetToReport = U2.AssetToReport) AS TotalCount, " +
                "(SELECT COUNT(VulnId) FROM Vulnerability NATURAL JOIN  UniqueFinding U2 WHERE VulnId = '21745' AND U1.AssetToReport = U2.AssetToReport) AS CountOf21745, " +
                "(SELECT COUNT(VulnId) FROM Vulnerability NATURAL JOIN  UniqueFinding U2 WHERE VulnId = '26917' AND U1.AssetToReport = U2.AssetToReport) AS CountOf26917 " +
                "FROM UniqueFinding U1 " +
                "NATURAL JOIN FindingTypes NATURAL JOIN Groups NATURAL JOIN FileNames " +
                "WHERE FindingType = @FindingType GROUP BY AssetToReport ORDER BY AssetToReport;";
            using (SQLiteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
            {
                while (sqliteDataReader.Read())
                {
                    AssetOverviewLineItem assetOverviewLineItem = new AssetOverviewLineItem(sqliteDataReader["AssetToReport"].ToString());
                    assetOverviewLineItem.HostName = sqliteDataReader["HostName"].ToString();
                    assetOverviewLineItem.IpAddress = sqliteDataReader["IpAddress"].ToString();
                    assetOverviewLineItem.FileName = sqliteDataReader["FileName"].ToString();
                    assetOverviewLineItem.GroupName = sqliteDataReader["GroupName"].ToString();
                    assetOverviewLineItem.OperatingSystem = sqliteDataReader["OperatingSystem"].ToString();
                    assetOverviewLineItem.CatI = (int.Parse(sqliteDataReader["CriticalCount"].ToString()) +
                        int.Parse(sqliteDataReader["HighCount"].ToString()) +
                        int.Parse(sqliteDataReader["CatICount"].ToString()));
                    assetOverviewLineItem.CatII = (int.Parse(sqliteDataReader["MediumCount"].ToString()) +
                        int.Parse(sqliteDataReader["CatIICount"].ToString()));
                    assetOverviewLineItem.CatIII = (int.Parse(sqliteDataReader["LowCount"].ToString()) +
                        int.Parse(sqliteDataReader["CatIIICount"].ToString()));
                    assetOverviewLineItem.CatIV = (int.Parse(sqliteDataReader["InfoCount"].ToString()) +
                        int.Parse(sqliteDataReader["CatIVCount"].ToString()));
                    assetOverviewLineItem.Total = (int.Parse(sqliteDataReader["TotalCount"].ToString()));
                    if (int.Parse(sqliteDataReader["CountOf21745"].ToString()) != 0 || int.Parse(sqliteDataReader["CountOf26917"].ToString()) != 0)
                    {
                        assetOverviewLineItem.Credentialed = "No";
                        if (int.Parse(sqliteDataReader["CountOf21745"].ToString()) != 0)
                        { assetOverviewLineItem.Credentialed = assetOverviewLineItem.Credentialed + "; 21745"; }
                        if (int.Parse(sqliteDataReader["CountOf26917"].ToString()) != 0)
                        { assetOverviewLineItem.Credentialed = assetOverviewLineItem.Credentialed + "; 26917"; }
                    }
                    else
                    { assetOverviewLineItem.Credentialed = "Yes"; }
                    assetList.Add(assetOverviewLineItem);
                }
            }
            return assetList;
        }

        private List<AssetOverviewLineItem> IncrementAssetVulnerabilityCounts(List<AssetOverviewLineItem> assetList, SQLiteCommand sqliteCommand)
        {
            sqliteCommand.CommandText = "SELECT AssetToReport, RawRisk, Impact FROM UniqueFinding " +
                "NATURAL JOIN FindingTypes NATURAL JOIN Vulnerability WHERE FindingType = @FindingType;";
            using (SQLiteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                while (sqliteDataReader.Read())
                {
                    AssetOverviewLineItem assetOverviewLineItem = assetList.FirstOrDefault(
                        x => x.AssetToReport.Equals(sqliteDataReader["AssetToReport"].ToString()));
                    if (assetOverviewLineItem != null)
                    {
                        string severity = string.Empty;
                        if (!String.IsNullOrWhiteSpace(sqliteDataReader["RawRisk"].ToString()))
                        { severity = sqliteDataReader["RawRisk"].ToString(); }
                        else
                        { severity = ConvertAcasSeverityToDisaCategory(sqliteDataReader["Impact"].ToString()); }
                        switch (severity)
                        {
                            case "I":
                                {
                                    assetOverviewLineItem.IncreaseCatIAndTotalCounts();
                                    break;
                                }
                            case "II":
                                {
                                    assetOverviewLineItem.IncreaseCatIIAndTotalCounts();
                                    break;
                                }
                            case "III":
                                {
                                    assetOverviewLineItem.IncreaseCatIIIAndTotalCounts();
                                    break;
                                }
                            case "IV":
                                {
                                    assetOverviewLineItem.IncreaseCatIVAndTotalCounts();
                                    break;
                                }
                            default:
                                { break; }
                        }
                    }
                }
            }
            return assetList;
        }

        private List<AssetOverviewLineItem> SetAssetCredentialedValue(List<AssetOverviewLineItem> assetList, SQLiteCommand sqliteCommand)
        {
            sqliteCommand.CommandText = "SELECT AssetToReport FROM UniqueFinding " +
                "NATURAL JOIN FindingTypes NATURAL JOIN Vulnerability WHERE FindingType = @FindingType AND VulnId = '21745';";
            using (SQLiteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
            {
                while (sqliteDataReader.Read())
                {
                    AssetOverviewLineItem assetOverviewLineItem = assetList.FirstOrDefault(
                            x => x.AssetToReport.Equals(sqliteDataReader["AssetToReport"].ToString()));
                    if (assetOverviewLineItem != null)
                    { assetOverviewLineItem.plugin21745Found = true; }
                }
            }

            sqliteCommand.CommandText = "SELECT AssetToReport FROM UniqueFinding " +
                "NATURAL JOIN FindingTypes NATURAL JOIN Vulnerability WHERE FindingType = @FindingType AND VulnId = '26917';";
            using (SQLiteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
            {
                while (sqliteDataReader.Read())
                {
                    AssetOverviewLineItem assetOverviewLineItem = assetList.FirstOrDefault(
                            x => x.AssetToReport.Equals(sqliteDataReader["AssetToReport"].ToString()));
                    if (assetOverviewLineItem != null)
                    { assetOverviewLineItem.plugin26917Found = true; }
                }
            }

            foreach (AssetOverviewLineItem asset in assetList)
            {
                if (asset.plugin21745Found || asset.plugin26917Found)
                {
                    asset.Credentialed = "No";
                    if (asset.plugin21745Found)
                    { asset.Credentialed = asset.Credentialed + "; 21745"; }
                    if (asset.plugin26917Found)
                    { asset.Credentialed = asset.Credentialed + "; 26917"; }
                }
                else
                { asset.Credentialed = "Yes"; } 
            }
            return assetList;
        }

        private void WriteAssetOverviewRow(AssetOverviewLineItem asset, string findingType)
        {
            assetOverviewOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(assetOverviewOpenXmlWriter, asset.HostName, 18);
            WriteCellValue(assetOverviewOpenXmlWriter, asset.IpAddress, 18);
            WriteCellValue(assetOverviewOpenXmlWriter, asset.GroupName, 18);
            WriteCellValue(assetOverviewOpenXmlWriter, asset.OperatingSystem, 18);
            WriteCellValue(assetOverviewOpenXmlWriter, asset.FileName, 18);
            WriteCellValue(assetOverviewOpenXmlWriter, asset.CatI.ToString(), 11);
            WriteCellValue(assetOverviewOpenXmlWriter, asset.CatII.ToString(), 12);
            WriteCellValue(assetOverviewOpenXmlWriter, asset.CatIII.ToString(), 13);
            WriteCellValue(assetOverviewOpenXmlWriter, asset.CatIV.ToString(), 14);
            WriteCellValue(assetOverviewOpenXmlWriter, asset.Total.ToString(), 18);
            switch (findingType)
            {
                case "ACAS":
                    {
                        WriteCellValue(assetOverviewOpenXmlWriter, asset.Credentialed, 18);
                        break;
                    }
                case "XCCDF":
                    {
                        WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 18);
                        break;
                    }
                default:
                    {
                        WriteCellValue(assetOverviewOpenXmlWriter, string.Empty, 18);
                        break;
                    }
            }
            assetOverviewOpenXmlWriter.WriteEndElement();
            assetOverviewRowCounterIndex++;
        }

        private void EndAssetOverview()
        {
            assetOverviewOpenXmlWriter.WriteEndElement();
            WriteAssetOverviewMergeCells();
            assetOverviewOpenXmlWriter.WriteEndElement();
            assetOverviewOpenXmlWriter.Dispose();
        }

        private void WriteAssetOverviewMergeCells()
        {
            assetOverviewMergeCellReferences.Add("A1:K1");
            assetOverviewOpenXmlWriter.WriteStartElement(new MergeCells());
            foreach (string reference in assetOverviewMergeCellReferences)
            { assetOverviewOpenXmlWriter.WriteElement(new MergeCell() { Reference = reference }); }
            assetOverviewOpenXmlWriter.WriteEndElement();
        }

        #endregion Create Asset Overview

        #region Create POA&M

        private void StartPoam(WorkbookPart workbookPart, Sheets sheets)
        {
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetIndex, Name = "POA&M" };
            sheetIndex++;
            sheets.Append(sheet);
            poamOpenXmlWriter = OpenXmlWriter.Create(worksheetPart);
            poamOpenXmlWriter.WriteStartElement(new Worksheet());
            WritePoamColumns();
            poamOpenXmlWriter.WriteStartElement(new SheetData());
            poamOpenXmlWriter.WriteElement(new Row() { Hidden = true });
            WritePoamHeaderRowOne();
            WritePoamHeaderRowTwo();
            WritePoamHeaderRowThree();
            WritePoamHeaderRowFour();
            WritePoamHeaderRowFive();
            WritePoamHeaderRowSix();

        }

        private void WritePoamColumns()
        {
            poamOpenXmlWriter.WriteStartElement(new Columns());
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 4.43, Max = 1, Min = 1 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 32.29, Max = 2, Min = 2 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 24.43, Max = 3, Min = 3 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 24.43, Max = 4, Min = 4 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 10.57, Max = 5, Min = 5 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 10.57, Max = 6, Min = 6 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 24.43, Max = 7, Min = 7 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 10.57, Max = 8, Min = 8 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 24.43, Max = 9, Min = 9 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 24.43, Max = 10, Min = 10 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 24.43, Max = 11, Min = 11 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 24.43, Max = 12, Min = 12 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 24.43, Max = 13, Min = 13 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 10.57, Max = 14, Min = 14 });
            poamOpenXmlWriter.WriteElement(new Column() { CustomWidth = true, Width = 32.29, Max = 15, Min = 15 });
            poamOpenXmlWriter.WriteEndElement();
        }

        private void WritePoamHeaderRowOne()
        {
            poamOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(poamOpenXmlWriter, "Date Exported:", 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, DateTime.Now.ToLongDateString(), 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, "System Type:", 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, "OMB Project ID:", 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            poamOpenXmlWriter.WriteEndElement();
        }

        private void WritePoamHeaderRowTwo()
        {
            poamOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(poamOpenXmlWriter, "Exported By:", 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, ContactName, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            poamOpenXmlWriter.WriteEndElement();
        }

        private void WritePoamHeaderRowThree()
        {
            poamOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(poamOpenXmlWriter, "DoD Component:", 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, "POC Name:", 19);
            WriteCellValue(poamOpenXmlWriter, ContactName, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            poamOpenXmlWriter.WriteEndElement();
        }

        private void WritePoamHeaderRowFour()
        {
            poamOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(poamOpenXmlWriter, @"System / Project Name:", 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, ContactName, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, "POC Phone:", 19);
            WriteCellValue(poamOpenXmlWriter, ContactNumber, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, "Security Costs:", 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            poamOpenXmlWriter.WriteEndElement();
        }

        private void WritePoamHeaderRowFive()
        {
            poamOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(poamOpenXmlWriter, "DoD IT Registration No:", 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, "POC Email:", 19);
            WriteCellValue(poamOpenXmlWriter, ContactEmail, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 19);
            poamOpenXmlWriter.WriteEndElement();
        }

        private void WritePoamHeaderRowSix()
        {
            poamOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(poamOpenXmlWriter, "Control Vulnerability Description", 16);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 16);
            WriteCellValue(poamOpenXmlWriter, @"Security Control Number (NC/NA controls only)", 16);
            WriteCellValue(poamOpenXmlWriter, @"Office/Org", 16);
            WriteCellValue(poamOpenXmlWriter, "Security Checks", 16);
            WriteCellValue(poamOpenXmlWriter, "Raw Severity Value", 16);
            WriteCellValue(poamOpenXmlWriter, "Mitigations", 16);
            WriteCellValue(poamOpenXmlWriter, "Severity Value", 16);
            WriteCellValue(poamOpenXmlWriter, "Resources Required", 16);
            WriteCellValue(poamOpenXmlWriter, "Scheduled Completion Date", 16);
            WriteCellValue(poamOpenXmlWriter, "Milestone with Completion Dates", 16);
            WriteCellValue(poamOpenXmlWriter, "Milestone Changes", 16);
            WriteCellValue(poamOpenXmlWriter, "Source Identifying Control Vulnerability", 16);
            WriteCellValue(poamOpenXmlWriter, "Status", 16);
            WriteCellValue(poamOpenXmlWriter, "Comments", 16);
            poamOpenXmlWriter.WriteEndElement();
        }

        private void WriteFindingToPoam(SQLiteDataReader sqliteDataReader, AsyncObservableCollection<MitigationItem> mitigationList)
        {
            MitigationItem mitigation = mitigationList.FirstOrDefault(
                x => x.MitigationGroupName.Equals(sqliteDataReader["GroupName"].ToString()) && 
                    x.MitigationVulnerabilityId.Equals(sqliteDataReader["VulnId"].ToString()));

            if (mitigation != null)
            {
                if (!FilterByStatus(mitigation.MitigationStatus))
                { return; }
            }
            else if (!FilterByStatus(sqliteDataReader["Status"].ToString()))
            { return; }

            poamOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(poamOpenXmlWriter, poamRowCounterIndex.ToString(), 16);
            string descriptionCellValue = "Title: " + Environment.NewLine + sqliteDataReader["VulnTitle"].ToString() + doubleCarriageReturn +
                "Description: " + Environment.NewLine + sqliteDataReader["Description"].ToString() + doubleCarriageReturn + 
                "Devices Affected:" + Environment.NewLine + sqliteDataReader["AssetIdToReport"].ToString().Replace(",", Environment.NewLine);
            descriptionCellValue = NormalizeCellValue(descriptionCellValue);
            descriptionCellValue = LargeCellValueHandler(descriptionCellValue, sqliteDataReader["VulnId"].ToString(), 
                sqliteDataReader["AssetIdToReport"].ToString().Replace(",", Environment.NewLine));
            WriteCellValue(poamOpenXmlWriter, descriptionCellValue, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 24);
            WriteCellValue(poamOpenXmlWriter, ContactOrganization + ", " + ContactName + ", " + ContactNumber + ", " + ContactEmail, 20);
            WriteCellValue(poamOpenXmlWriter, sqliteDataReader["VulnId"].ToString(), 24);
            if (!String.IsNullOrWhiteSpace(sqliteDataReader["RawRisk"].ToString()))
            { WriteCellValue(poamOpenXmlWriter, sqliteDataReader["RawRisk"].ToString(), 24); }
            else
            { WriteCellValue(poamOpenXmlWriter, ConvertAcasSeverityToDisaCategory(sqliteDataReader["Impact"].ToString()), 24); }
            if (mitigation != null)
            { WriteCellValue(poamOpenXmlWriter, mitigation.MitigationText, 20); }
            else
            { WriteCellValue(poamOpenXmlWriter, sqliteDataReader["FindingDetails"].ToString(), 20); }
            WriteCellValue(poamOpenXmlWriter, string.Empty, 24);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, string.Empty, 20);
            WriteCellValue(poamOpenXmlWriter, sqliteDataReader["Source"].ToString(), 24);
            if (mitigation != null)
            { WriteCellValue(poamOpenXmlWriter, mitigation.MitigationStatus, 24); }
            else
            { WriteCellValue(poamOpenXmlWriter, sqliteDataReader["Status"].ToString(), 24); }
            /*WriteCellValue(poamOpenXmlWriter, sqliteDataReader["Comments"].ToString(), 20);*/
            WriteCellValue(poamOpenXmlWriter, sqliteDataReader["AssetIdToReport"].ToString().Replace(",", Environment.NewLine), 20);
            poamOpenXmlWriter.WriteEndElement();
            poamRowCounterIndex++;
        }

        private void EndPoam()
        {
            poamOpenXmlWriter.WriteEndElement();
            WritePoamMergeCells();
            poamOpenXmlWriter.WriteEndElement();
            poamOpenXmlWriter.Dispose();
        }

        private void WritePoamMergeCells()
        {
            string[] mergeCellArray = new string[] { 
                "A1:O1", "A2:B2", "A3:B3", "A4:B4", "A5:B5", "A6:B6", "A7:B7", "C2:H2", "C3:H3", 
                "C4:H4", "C5:H5", "C6:H6", "I2:I3", "J2:K3", "J4:K4", "J5:K5", "J6:K6", "L2:L3", 
                "M2:O3", "M5:O5", "L4:O4", "L6:O6" };
            poamOpenXmlWriter.WriteStartElement(new MergeCells());
            foreach (string mergeCell in mergeCellArray)
            { poamOpenXmlWriter.WriteElement(new MergeCell() { Reference = mergeCell }); }
            poamOpenXmlWriter.WriteEndElement();
        }

        #endregion Create POA&M

        #region Create RAR

        private void StartRar(WorkbookPart workbookPart, Sheets sheets)
        {
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetIndex, Name = "RAR" };
            sheetIndex++;
            sheets.Append(sheet);
            rarOpenXmlWriter = OpenXmlWriter.Create(worksheetPart);
            rarOpenXmlWriter.WriteStartElement(new Worksheet());
            WriteRarColumns();
            rarOpenXmlWriter.WriteStartElement(new SheetData());
            WriteRarHeaderRowOne();
            WriteRarHeaderRowTwo();
            WriteRarHeaderRowThree();
            WriteRarHeaderRowFour();
            WriteRarHeaderRowFive();
            WriteRarHeaderRowSix();
        }

        private void WriteRarColumns()
        {
            rarOpenXmlWriter.WriteStartElement(new Columns());
            rarOpenXmlWriter.WriteElement(new Column() { Min = 1U, Max = 1U, Width = 16.86d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 2U, Max = 2U, Width = 16.86d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 3U, Max = 3U, Width = 16.86d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 4U, Max = 4U, Width = 31.86d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 5U, Max = 5U, Width = 13.71d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 6U, Max = 6U, Width = 14.14d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 7U, Max = 7U, Width = 13.00d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 8U, Max = 8U, Width = 23.14d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 9U, Max = 9U, Width = 23.14d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 10U, Max = 10U, Width = 13.00d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 11U, Max = 11U, Width = 17.29d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 12U, Max = 12U, Width = 17.29d, CustomWidth = true });
            rarOpenXmlWriter.WriteElement(new Column() { Min = 13U, Max = 13U, Width = 31.86d, CustomWidth = true });
            rarOpenXmlWriter.WriteEndElement();
        }

        private void WriteRarHeaderRowOne()
        {
            rarOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(rarOpenXmlWriter, "System Name and Version:", 23);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 23);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
            rarOpenXmlWriter.WriteEndElement();
        }

        private void WriteRarHeaderRowTwo()
        {
            rarOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(rarOpenXmlWriter, "Date(s) of Testing:", 23);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 23);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
            rarOpenXmlWriter.WriteEndElement();
        }

        private void WriteRarHeaderRowThree()
        {
            rarOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(rarOpenXmlWriter, "Date of Report:", 23);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 23);
            WriteCellValue(rarOpenXmlWriter, DateTime.Now.ToLongDateString(), 20);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
            rarOpenXmlWriter.WriteEndElement();
        }

        private void WriteRarHeaderRowFour()
        {
            string contactInfo = ContactName + ", " + ContactNumber + ", " + ContactEmail;
            rarOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(rarOpenXmlWriter, "POC Information:", 23);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 23);
            WriteCellValue(rarOpenXmlWriter, contactInfo, 20);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
            rarOpenXmlWriter.WriteEndElement();
        }

        private void WriteRarHeaderRowFive()
        {
            rarOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(rarOpenXmlWriter, "Risk Assessment Method:", 23);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 23);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
            rarOpenXmlWriter.WriteEndElement();
        }

        private void WriteRarHeaderRowSix()
        {
            rarOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(rarOpenXmlWriter, "Identifier Applicable Security Control (1)", 15);
            WriteCellValue(rarOpenXmlWriter, "Source of Discovery or Test Tool Name (2)", 15);
            WriteCellValue(rarOpenXmlWriter, "Test ID or Threat IDs (3)", 15);
            WriteCellValue(rarOpenXmlWriter, @"Description of Vulnerability / Weakness (4)", 15);
            WriteCellValue(rarOpenXmlWriter, "Raw Risk (CAT I, II, III) (5)", 15);
            WriteCellValue(rarOpenXmlWriter, "Impact (6)", 15);
            WriteCellValue(rarOpenXmlWriter, "Likelihood (7)", 15);
            WriteCellValue(rarOpenXmlWriter, "Mitigation Description (8)", 15);
            WriteCellValue(rarOpenXmlWriter, "Remediation Description (9)", 15);
            WriteCellValue(rarOpenXmlWriter, "Residual Risk (10)", 15);
            WriteCellValue(rarOpenXmlWriter, "Status (11)", 15);
            WriteCellValue(rarOpenXmlWriter, "Comment (12)", 15);
            WriteCellValue(rarOpenXmlWriter, "Devices Affected (13)", 15);
            rarOpenXmlWriter.WriteEndElement();
        }

        private void WriteFindingToRar(SQLiteDataReader sqliteDataReader, AsyncObservableCollection<MitigationItem> mitigationList)
        {
            MitigationItem mitigation = mitigationList.FirstOrDefault(
                x => x.MitigationGroupName.Equals(sqliteDataReader["GroupName"].ToString()) &&
                    x.MitigationVulnerabilityId.Equals(sqliteDataReader["VulnId"].ToString()));

            if (mitigation != null)
            {
                if (!FilterByStatus(mitigation.MitigationStatus))
                { return; }
            }
            else if (!FilterByStatus(sqliteDataReader["Status"].ToString()))
            { return; }
            
            rarOpenXmlWriter.WriteStartElement(new Row());
            if (IsDiacapPackage)
            { WriteCellValue(rarOpenXmlWriter, sqliteDataReader["IaControl"].ToString(), 24);}
            else
            { WriteCellValue(rarOpenXmlWriter, sqliteDataReader["CciReference"].ToString(), 24); }
            WriteCellValue(rarOpenXmlWriter, sqliteDataReader["Source"].ToString(), 24);
            WriteCellValue(rarOpenXmlWriter, sqliteDataReader["VulnId"].ToString(), 24);
            WriteCellValue(rarOpenXmlWriter, sqliteDataReader["VulnTitle"].ToString(), 20);
            if (!String.IsNullOrWhiteSpace(sqliteDataReader["RawRisk"].ToString()))
            { WriteCellValue(rarOpenXmlWriter, sqliteDataReader["RawRisk"].ToString(), 24); }
            else
            { WriteCellValue(rarOpenXmlWriter, ConvertAcasSeverityToDisaCategory(sqliteDataReader["Impact"].ToString()), 24); }
            WriteCellValue(rarOpenXmlWriter, sqliteDataReader["Impact"].ToString(), 24);
            WriteCellValue(rarOpenXmlWriter, string.Empty, 24);
            if (mitigation != null && mitigation.MitigationStatus.Equals("Completed"))
            {
                WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
                WriteCellValue(rarOpenXmlWriter, mitigation.MitigationText, 20);
                WriteCellValue(rarOpenXmlWriter, string.Empty, 24);
                WriteCellValue(rarOpenXmlWriter, mitigation.MitigationStatus, 24);
            }
            else if (mitigation != null)
            {
                WriteCellValue(rarOpenXmlWriter, mitigation.MitigationText, 20);
                WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
                WriteCellValue(rarOpenXmlWriter, string.Empty, 24);
                WriteCellValue(rarOpenXmlWriter, mitigation.MitigationStatus, 24);
            }
            else if (sqliteDataReader["Status"].ToString().Equals("Completed"))
            {
                WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
                WriteCellValue(rarOpenXmlWriter, sqliteDataReader["Comments"].ToString(), 20);
                WriteCellValue(rarOpenXmlWriter, string.Empty, 24);
                WriteCellValue(rarOpenXmlWriter, sqliteDataReader["Status"].ToString(), 24);
            }
            else
            {
                WriteCellValue(rarOpenXmlWriter, sqliteDataReader["Comments"].ToString(), 20);
                WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
                WriteCellValue(rarOpenXmlWriter, string.Empty, 24);
                WriteCellValue(rarOpenXmlWriter, sqliteDataReader["Status"].ToString(), 24);
            }
            WriteCellValue(rarOpenXmlWriter, string.Empty, 20);
            WriteCellValue(rarOpenXmlWriter, sqliteDataReader["AssetIdToReport"].ToString().Replace(",", Environment.NewLine), 20);
            rarOpenXmlWriter.WriteEndElement();
        }

        private void EndRar()
        {
            rarOpenXmlWriter.WriteEndElement();
            WriteRarMergeCells();
            rarOpenXmlWriter.WriteEndElement();
            rarOpenXmlWriter.Dispose();
        }

        private void WriteRarMergeCells()
        {
            string[] mergeCellArray = new string[] {
                "A1:B1", "A2:B2", "A3:B3", "A4:B4", "A5:B5",
                "C1:D1", "C2:D2", "C3:D3", "C4:D4", "C5:D5", "E1:M5"};
            rarOpenXmlWriter.WriteStartElement(new MergeCells());
            foreach (string mergeCell in mergeCellArray)
            { rarOpenXmlWriter.WriteElement(new MergeCell() { Reference = mergeCell }); }
            rarOpenXmlWriter.WriteEndElement();
        }

        #endregion Create RAR

        #region Create ACAS Output

        private void StartAcasOutput(WorkbookPart workbookPart, Sheets sheets)
        {
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetIndex, Name = "ACAS Output" };
            sheetIndex++;
            sheets.Append(sheet);
            acasOutputOpenXmlWriter = OpenXmlWriter.Create(worksheetPart);
            acasOutputOpenXmlWriter.WriteStartElement(new Worksheet());
            WriteAcasOutputColumns();
            acasOutputOpenXmlWriter.WriteStartElement(new SheetData());
            WriteAcasOutputHeaderRow();
        }

        private void WriteAcasOutputColumns()
        {
            acasOutputOpenXmlWriter.WriteStartElement(new Columns());
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 1U, Max = 1U, Width = 10.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 2U, Max = 2U, Width = 20.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 3U, Max = 3U, Width = 15.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 4U, Max = 4U, Width = 15.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 5U, Max = 5U, Width = 20.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 6U, Max = 6U, Width = 20.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 7U, Max = 7U, Width = 35.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 8U, Max = 8U, Width = 35.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 9U, Max = 9U, Width = 35.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 10U, Max = 10U, Width = 35.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 11U, Max = 11U, Width = 20.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 12U, Max = 12U, Width = 35.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 13U, Max = 13U, Width = 25.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 14U, Max = 14U, Width = 25.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 15U, Max = 15U, Width = 25.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 16U, Max = 16U, Width = 25.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteElement(new Column() { Min = 17U, Max = 17U, Width = 25.00d, CustomWidth = true });
            acasOutputOpenXmlWriter.WriteEndElement();
        }

        private void WriteAcasOutputHeaderRow()
        {
            acasOutputOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(acasOutputOpenXmlWriter, "Plugin ID", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Plugin Title", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Risk Factor", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "STIG Severity", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Asset Name", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "IP Address", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Synopsis", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Description", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Scan Output", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Solution", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Cross Reference", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "CPE", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Plugin Publication Date", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Plugin Modification Date", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Last Observed Date", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Patch Publication Date", 4);
            WriteCellValue(acasOutputOpenXmlWriter, "Scan Filename", 4);
            acasOutputOpenXmlWriter.WriteEndElement();
        }

        private void WriteIndividualAcasOutput()
        {
            using (SQLiteCommand sqliteCommand = FindingsDatabaseActions.sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = ";";
            }
        }

        private void EndAcasOutput()
        {
            acasOutputOpenXmlWriter.WriteEndElement();
            acasOutputOpenXmlWriter.WriteEndElement();
            acasOutputOpenXmlWriter.Dispose();
        }

        #endregion Create Acas Output

        #region Create Discrepancies

        private void StartDiscrepancies(WorkbookPart workbookPart, Sheets sheets)
        {
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetIndex, Name = @"Discrepancies" };
            sheetIndex++;
            sheets.Append(sheet);
            discrepanciesOpenXmlWriter = OpenXmlWriter.Create(worksheetPart);
            discrepanciesOpenXmlWriter.WriteStartElement(new Worksheet());
            WriteDiscrepanciesColumns();
            discrepanciesOpenXmlWriter.WriteStartElement(new SheetData());
            WriteDiscrepanciesTabHeaderRow();
        }

        private void WriteDiscrepanciesColumns()
        {
            discrepanciesOpenXmlWriter.WriteStartElement(new Columns());
            discrepanciesOpenXmlWriter.WriteElement(new Column() { Min = 1U, Max = 1U, Width = 30d, CustomWidth = true });
            discrepanciesOpenXmlWriter.WriteElement(new Column() { Min = 2U, Max = 2U, Width = 10d, CustomWidth = true });
            discrepanciesOpenXmlWriter.WriteElement(new Column() { Min = 3U, Max = 3U, Width = 30d, CustomWidth = true });
            discrepanciesOpenXmlWriter.WriteElement(new Column() { Min = 4U, Max = 4U, Width = 15d, CustomWidth = true });
            discrepanciesOpenXmlWriter.WriteElement(new Column() { Min = 5U, Max = 5U, Width = 15d, CustomWidth = true });
            discrepanciesOpenXmlWriter.WriteElement(new Column() { Min = 6U, Max = 6U, Width = 55d, CustomWidth = true });
            discrepanciesOpenXmlWriter.WriteElement(new Column() { Min = 7U, Max = 7U, Width = 55d, CustomWidth = true });
            discrepanciesOpenXmlWriter.WriteElement(new Column() { Min = 8U, Max = 8U, Width = 55d, CustomWidth = true });
            discrepanciesOpenXmlWriter.WriteElement(new Column() { Min = 9U, Max = 9U, Width = 55d, CustomWidth = true });
            discrepanciesOpenXmlWriter.WriteElement(new Column() { Min = 10U, Max = 10U, Width = 20d, CustomWidth = true });
            discrepanciesOpenXmlWriter.WriteElement(new Column() { Min = 11U, Max = 11U, Width = 20d, CustomWidth = true });
            discrepanciesOpenXmlWriter.WriteEndElement();
        }

        private void WriteDiscrepanciesTabHeaderRow()
        {
            discrepanciesOpenXmlWriter.WriteStartElement(new Row());
            WriteCellValue(discrepanciesOpenXmlWriter, "Source", 4);
            WriteCellValue(discrepanciesOpenXmlWriter, "Vuln ID", 4);
            WriteCellValue(discrepanciesOpenXmlWriter, "Vuln Title", 4);
            WriteCellValue(discrepanciesOpenXmlWriter, "STIG Status", 4);
            WriteCellValue(discrepanciesOpenXmlWriter, "SCAP Status", 4);
            WriteCellValue(discrepanciesOpenXmlWriter, "STIG Comments", 4);
            WriteCellValue(discrepanciesOpenXmlWriter, "STIG Finding Details", 4);
            WriteCellValue(discrepanciesOpenXmlWriter, "STIG File Name", 4);
            WriteCellValue(discrepanciesOpenXmlWriter, "SCAP File Name", 4);
            WriteCellValue(discrepanciesOpenXmlWriter, "STIG Affected Asset", 4);
            WriteCellValue(discrepanciesOpenXmlWriter, "SCAP Affected Asset", 4);
            discrepanciesOpenXmlWriter.WriteEndElement();
        }

        private void EndDiscrepancies()
        {
            discrepanciesOpenXmlWriter.WriteEndElement();
            discrepanciesOpenXmlWriter.WriteEndElement();
            discrepanciesOpenXmlWriter.Dispose();
        }

        #endregion Create Discrepancies

        #region Handle Cell Data

        private void WriteCellValue(OpenXmlWriter openXmlWriter, string cellValue, int styleIndex)
        {
            List<OpenXmlAttribute> openXmlAttributes = new List<OpenXmlAttribute>();
            openXmlAttributes.Add(new OpenXmlAttribute("s", null, styleIndex.ToString()));
            openXmlAttributes.Add(new OpenXmlAttribute("t", null, "s"));
            openXmlWriter.WriteStartElement(new Cell(), openXmlAttributes);
            if (!sharedStringDictionary.ContainsKey(cellValue))
            {
                sharedStringDictionary.Add(cellValue, sharedStringMaxIndex);
                sharedStringMaxIndex++;
            }
            openXmlWriter.WriteElement(new CellValue(sharedStringDictionary[cellValue].ToString()));     
            openXmlWriter.WriteEndElement();
        }

        private void CreateSharedStringPart(WorkbookPart workbookPart)
        {
            if (sharedStringMaxIndex > 0)
            {
                SharedStringTablePart sharedStringTablePart = workbookPart.AddNewPart<SharedStringTablePart>();
                using (OpenXmlWriter openXmlWriter = OpenXmlWriter.Create(sharedStringTablePart))
                {
                    openXmlWriter.WriteStartElement(new SharedStringTable());
                    foreach (var item in sharedStringDictionary)
                    {
                        openXmlWriter.WriteStartElement(new SharedStringItem());
                        openXmlWriter.WriteElement(new Text(item.Key));
                        openXmlWriter.WriteEndElement();
                    }
                    openXmlWriter.WriteEndElement();
                }
            }
        }

        #endregion Handle Cell Data

        #region Data Preparation

        private bool FilterBySeverity(string impact, string rawRisk)
        {
            bool riskFactorMatch = true;
            bool stigSeverityMatch = true;
            bool riskFactorToStigSeverityCrossRef = true;
            if (!String.IsNullOrWhiteSpace(impact))
            {
                switch (impact)
                {
                    case "Critical":
                        { riskFactorMatch = IncludeCriticalFindings; riskFactorToStigSeverityCrossRef = IncludCatIFindings; break; }
                    case "High":
                        { riskFactorMatch = IncludeHighFindings; riskFactorToStigSeverityCrossRef = IncludCatIFindings; break; }
                    case "Medium":
                        { riskFactorMatch = IncludeMediumFindings; riskFactorToStigSeverityCrossRef = IncludCatIIFindings; break; }
                    case "Low":
                        { riskFactorMatch = IncludeLowFindings; riskFactorToStigSeverityCrossRef = IncludCatIIIFindings; break; }
                    case "Informational":
                        { riskFactorMatch = IncludeInformationalFindings; riskFactorToStigSeverityCrossRef = IncludCatIVFindings; break; }
                    default:
                        { break; }
                }
            }
            if (!String.IsNullOrWhiteSpace(rawRisk))
            {
                switch (rawRisk)
                {
                    case "I":
                        { stigSeverityMatch = IncludCatIFindings; break; }
                    case "II":
                        { stigSeverityMatch = IncludCatIIFindings; break; }
                    case "III":
                        { stigSeverityMatch = IncludCatIIIFindings; break; }
                    case "IV":
                        { stigSeverityMatch = IncludCatIVFindings; break; }
                    default:
                        { break; }
                }
            }
            if (!riskFactorMatch || !stigSeverityMatch || !riskFactorToStigSeverityCrossRef)
            { return false; }
            else
            { return true; }
        }

        private bool FilterByStatus(string status)
        {
            bool severityMatch = true;
            switch (status)
            {
                case "Ongoing":
                    { severityMatch = IncludeOngoingFindings; break; }
                case "Completed":
                    { severityMatch = IncludeCompletedFindings; break; }
                case "Not Reviewed":
                    { severityMatch = IncludeNotReviewedFindings; break; }
                case "Not Applicable":
                    { severityMatch = IncludeNotApplicableFindings; break; }
                case "Unknown":
                    { break; }
                case "Undetermined":
                    { break; }
                default:
                    { break; }
            }
            if (!severityMatch)
            { return false; }
            else
            { return true; }
        }

        private string ConvertAcasSeverityToDisaCategory(string acasSeverity)
        {
            switch (acasSeverity)
            {
                case "Critical":
                    { return "I"; }
                case "High":
                    { return "I"; }
                case "Medium":
                    { return "II"; }
                case "Low":
                    { return "III"; }
                case "Informational":
                    { return "IV"; }
                default:
                    { return "Unknown"; }
            }
        }

        private string CompareIavmAgeToUserSettings(string iavmAge)
        {
            int iavmAgeAsInt = int.Parse(iavmAge);

            if (iavmAgeAsInt > IavmFilterFour)
            { return "> " + IavmFilterFour.ToString() + " Days"; }
            else if (iavmAgeAsInt > IavmFilterThree)
            { return "> " + IavmFilterThree.ToString() + " Days"; }
            else if (iavmAgeAsInt > IavmFilterTwo)
            { return "> " + IavmFilterTwo.ToString() + " Days"; }
            else if (iavmAgeAsInt > IavmFilterOne)
            { return "> " + IavmFilterOne.ToString() + " Days"; }
            else
            { return "< " + IavmFilterOne.ToString() + " Days"; }
        }

        private string NormalizeCellValue(string cellValue)
        {
            if (cellValue.Contains(">"))
            { cellValue.Replace(">", "&gt;"); }
            if (cellValue.Contains("<"))
            { cellValue.Replace("<", "&lt;"); }
            return cellValue;
        }

        private string LargeCellValueHandler(string cellValue, string pluginId, string assetName)
        {
            if (cellValue.Length < 32767)
            { return cellValue; }
            string outputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AcasScanOutput_TextFiles";
            string outputTextFile = string.Empty;
            if (cellValue.Contains("Title:"))
            { outputTextFile = outputPath + @"\" + pluginId + "_" + assetName + "_VulnerabilityDescription.txt"; }
            else
            { outputTextFile = outputPath + @"\" + pluginId + "_" + assetName + "_ScanOutput.txt"; }
            if (!Directory.Exists(outputPath))
            { Directory.CreateDirectory(outputPath); }
            if (!File.Exists(outputTextFile))
            {
                using (FileStream fs = new FileStream(outputTextFile, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(cellValue);
                    sw.Close();
                }
            }
            return "Output text exceeds the maximum character allowance for an Excel cell; " + 
                "please see \"" + outputTextFile + "\" for full output details.";
        }

        #endregion Data Preparation

        #region Create Stylesheet

        private Stylesheet CreateStylesheet()
        {
            HorizontalAlignmentValues leftHorizontal = HorizontalAlignmentValues.Left;
            HorizontalAlignmentValues rightHorizontal = HorizontalAlignmentValues.Right;
            HorizontalAlignmentValues centerHorizontal = HorizontalAlignmentValues.Center;
            VerticalAlignmentValues topVertical = VerticalAlignmentValues.Top;
            VerticalAlignmentValues centerVertical = VerticalAlignmentValues.Center;

            return new Stylesheet(
                new Fonts(
                /*Index 0 - Black*/ CreateFont("000000", false),
                /*Index 1 - Bold Black*/ CreateFont("000000", true),
                /*Index 2 - Purple*/ CreateFont("660066", false),
                /*Index 3 - Bold Purple*/ CreateFont("660066", true),
                /*Index 4 - Red*/ CreateFont("990000", false),
                /*Index 5 - Bold Red*/ CreateFont("990000", true),
                /*Index 6 - Orange*/ CreateFont("FF6600", false),
                /*Index 7 - Bold Orange*/ CreateFont("FF6600", true),
                /*Index 8 - Blue*/ CreateFont("0066FF", false),
                /*Index 9 - Bold Blue*/ CreateFont("066FF", true),
                /*Index 10 - Green*/ CreateFont("339900", false),
                /*Index 11 - Bold Green*/ CreateFont("339900", true),
                /*Index 12 - Bold Black Large*/ CreateFont("000000", true)
                    ),
                new Fills(
                /*Index 0 - Default Fill (None)*/ CreateFill(string.Empty, PatternValues.None),
                /*Index 1 - Default Fill (Gray125)*/ CreateFill(string.Empty, PatternValues.Gray125),
                /*Index 2 - Dark Gray Fill*/ CreateFill("BBBBBB", PatternValues.Solid),
                /*Index 3 - Light Gray Fill*/ CreateFill("EEEEEE", PatternValues.Solid),
                /*Index 4 - Yellow Gray Fill*/ CreateFill("FFCC00", PatternValues.Solid)
                    ),
                new Borders(
                /*Index 0 - Default Border (None)*/ CreateBorder(false, false, false, false),
                /*Index 1 - All Borders*/ CreateBorder(true, true, true, true),
                /*Index 2 - Top & Bottom Borders*/ CreateBorder(true, false, true, false)
                    ),
                new CellFormats(
                /*Index 0 - Black Font, No Fill, No Borders, Wrap Text*/ CreateCellFormat(0, 0, 0, leftHorizontal, null, true),
                /*Index 1 - Black Font, No Fill, No Borders, Horizontally Centered*/ CreateCellFormat(0, 0, 0, centerHorizontal, null, false),
                /*Index 2 - Bold Black Font, Dark Gray Fill, All Borders*/ CreateCellFormat(1, 2, 1, null, null, false),
                /*Index 3 - Bold Black Font, Dark Gray Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(1, 2, 2, centerHorizontal, centerVertical, false),
                /*Index 4 - Bold Black Font, Dark Gray Fill, All Borders, Centered*/ CreateCellFormat(1, 2, 1, centerHorizontal, centerVertical, false),
                /*Index 5 - Bold Purple Font, Light Gray Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(3, 3, 2, centerHorizontal, centerVertical, false),
                /*Index 6 - Bold Red Font, Light Gray Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(5, 3, 2, centerHorizontal, centerVertical, false),
                /*Index 7 - Bold Orange Font, Light Gray Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(7, 3, 2, centerHorizontal, centerVertical, false),
                /*Index 8 - Bold Blue Font, Light Gray Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(9, 3, 2, centerHorizontal, centerVertical, false),
                /*Index 9 - Bold Green Font, Light Gray Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(11, 3, 2, centerHorizontal, centerVertical, false),
                /*Index 10 - Purple Font, No Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(2, 0, 2, centerHorizontal, centerVertical, false),
                /*Index 11 - Red Font, No Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(4, 0, 2, centerHorizontal, centerVertical, false),
                /*Index 12 - Orange Font, No Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(6, 0, 2, centerHorizontal, centerVertical, false),
                /*Index 13 - BlueFont , No Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(8, 0, 2, centerHorizontal, centerVertical, false),
                /*Index 14 - Green Font, No Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(10, 0, 2, centerHorizontal, centerVertical, false),
                /*Index 15 - Bold Black Font, Yellow Fill, All Borders, Centered, Wrap Text*/ CreateCellFormat(1, 4, 1, centerHorizontal, centerVertical, true),
                /*Index 16 - Bold Black Font, No Fill, All Borders, Wrap Text*/ CreateCellFormat(1, 0, 1, centerHorizontal, centerVertical, true),
                /*Index 17 - Bold Black Font, Light Gray Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(1, 3, 2, centerHorizontal, centerVertical, false),
                /*Index 18 - Bold Black Font, No Fill, Top & Bottom Borders, Centered*/ CreateCellFormat(0, 0, 2, centerHorizontal, centerVertical, false),
                /*Index 19 - Bold Black Font, Dark Gray Fill, Top & Bottom Borders, Centered Vertically*/ CreateCellFormat(1, 2, 1, null, centerVertical, false),
                /*Index 20 - Black Font, No Fill, All Borders, Top Aligned, Wrap Text*/ CreateCellFormat(0, 0, 1, null, topVertical, true),
                /*Index 21 - Black Font, No Fill, All Borders, Centered Veritcally, Wrap Text*/ CreateCellFormat(0, 0, 1, null, centerVertical, true),
                /*Index 22 - Black Font, No Fill, All Borders, Centered, Wrap Text*/ CreateCellFormat(0, 0, 1, centerHorizontal, centerVertical, true),
                /*Index 23 - Black Font, No Fill, All Borders, Centered Vertically, Right Aligned*/ CreateCellFormat(0, 0, 1, rightHorizontal, centerVertical, false),
                /*Index 24 - Black Font, No Fill, All Borders, Centered Horizontally, Top Aligned, Wrap Text*/ CreateCellFormat(0, 0, 1, centerHorizontal, topVertical, true)
                    )
                );
        }

        private Font CreateFont(string fontColor, bool isBold)
        {
            Font font = new Font();
            font.FontSize = new FontSize() { Val = 10 };
            font.Color = new Color { Rgb = new HexBinaryValue() { Value = fontColor } };
            font.FontName = new FontName() { Val = "Calibri" };
            if (isBold)
            { font.Bold = new Bold(); }
            return font;
        }

        private Fill CreateFill(string fillColor, PatternValues patternValue)
        {
            Fill fill = new Fill();
            PatternFill patternfill = new PatternFill();
            patternfill.PatternType = patternValue;
            if (!String.IsNullOrWhiteSpace(fillColor))
            { patternfill.ForegroundColor = new ForegroundColor() { Rgb = new HexBinaryValue { Value = fillColor } }; }
            fill.PatternFill = patternfill;

            return fill;
        }

        private Border CreateBorder(bool topBorderRequired, bool rightBorderRequired, bool bottomBorderRequired, bool leftBorderRequired)
        {
            Border border = new Border();
            if (!topBorderRequired && !rightBorderRequired && !bottomBorderRequired && !leftBorderRequired)
            {
                border.TopBorder = new TopBorder();
                border.RightBorder = new RightBorder();
                border.BottomBorder = new BottomBorder();
                border.LeftBorder = new LeftBorder();
                border.DiagonalBorder = new DiagonalBorder();
            }
            else
            {
                if (topBorderRequired)
                { border.TopBorder = new TopBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }; }
                if (rightBorderRequired)
                { border.RightBorder = new RightBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }; }
                if (bottomBorderRequired)
                { border.BottomBorder = new BottomBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }; }
                if (leftBorderRequired)
                { border.LeftBorder = new LeftBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin }; }
                border.DiagonalBorder = new DiagonalBorder();
            }
            return border;
        }

        private CellFormat CreateCellFormat(UInt32Value fontId, UInt32Value fillId, UInt32Value borderId,
            HorizontalAlignmentValues? horizontalAlignment, VerticalAlignmentValues? verticalAlignment, bool wrapText)
        {
            CellFormat cellFormat = new CellFormat();
            Alignment alignment = new Alignment();
            if (horizontalAlignment != null)
            { alignment.Horizontal = horizontalAlignment; }
            if (verticalAlignment != null)
            { alignment.Vertical = verticalAlignment; }
            alignment.WrapText = wrapText;
            cellFormat.Alignment = alignment;
            cellFormat.FontId = fontId;
            cellFormat.FillId = fillId;
            cellFormat.BorderId = borderId;
            return cellFormat;
        }

        #endregion Create Stylesheet

        private string SetSqliteCommandTextForPoamAndRar(string findingType, bool isMerged)
        {
            if (isMerged)
            {
                return "SELECT GroupName, VulnId, VulnTitle, RawRisk, Impact, Description, IaControl, " +
                    "CciReference, Status, Source, Comments, FindingDetails, " +
                    "GROUP_CONCAT(DISTINCT AssetIdToReport) AS AssetIdToReport FROM UniqueFinding " +
                    "NATURAL JOIN FindingTypes NATURAL JOIN VulnerabilitySources " +
                    "NATURAL JOIN FindingStatuses NATURAL JOIN Vulnerability NATURAL JOIN Groups " +
                    "NATURAL JOIN Assets " +
                    "WHERE FindingType = @FindingType GROUP BY VulnId;";
            }
            else
            {
                return "SELECT * FROM UniqueFinding NATURAL JOIN FindingTypes " +
                    "NATURAL JOIN VulnerabilitySources NATURAL JOIN FindingStatuses NATURAL JOIN Assets " +
                    "NATURAL JOIN Vulnerability NATURAL JOIN Groups WHERE FindingType = @FindingType;";
            }
        }

        private void AddIavmsToObservableCollection(AsyncObservableCollection<Iavm> iavmList, DataRow acasFindingRow)
        {
            if (!String.IsNullOrWhiteSpace(acasFindingRow["IavmNumber"].ToString()))
            {
                iavmList.Add(new Iavm(false, acasFindingRow["IavmNumber"].ToString(), acasFindingRow["VulnId"].ToString(), acasFindingRow["VulnTitle"].ToString(),
                    acasFindingRow["HostName"].ToString(), CompareIavmAgeToUserSettings(acasFindingRow["Age"].ToString()), acasFindingRow["SystemName"].ToString()));
            }
        }
    }
}
