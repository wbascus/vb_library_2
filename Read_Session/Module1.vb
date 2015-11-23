﻿Imports Microsoft.Office.Interop
Imports System.Text.RegularExpressions

Module Module1
    Sub Main()
        Dim objExcel As Excel.Application       'the file we're going to read from
        Dim conn As ADODB.Connection
        Dim files_read = 0
        Dim new_files = 0
        Dim successful_adds = 0
        Dim error_format = 0
        Dim error_content = 0
        Dim results As Integer()
        Dim sSql
        Dim rec As ADODB.Recordset
        Dim start_time As DateTime
        Dim end_time As DateTime
        Dim elapsed_time As Long

        start_time = Now()

        results = {0, 0, 0, 0, 0, 0}
        rec = New ADODB.Recordset
        objExcel = New Excel.Application
        conn = New ADODB.Connection

        conn.Open("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\submissions\Session_responses.accdb")

        sSql = "DELETE * FROM submittals WHERE submittalID > 9228"
        'Debug.WriteLine(sSql)
        'conn.Execute(sSql)
        sSql = "DELETE * FROM responses"
        'Debug.WriteLine(sSql)
        'conn.Execute(sSql)
        sSql = "DELETE * FROM unit_correction_map"
        'Debug.WriteLine(sSql)
        'conn.Execute(sSql)
        sSql = "DELETE * FROM org_team_map"
        'Debug.WriteLine(sSql)
        'conn.Execute(sSql)

        results = process_Folder(objExcel, conn)

        'test a single file
        'Process_workbook("20151110-160531 Working in Workday Session 1 - Data Collection Tool College of Arts And Sciences_Linguistics" & ".xlsm", objExcel, conn)    'Session 1 test
        'Process_workbook("20151118- 1009 HFS - Session 3 - Data Collection - Working in Workday" & ".xlsx", objExcel, conn)    'Session 3 test
        'Process_workbook("20151118- 1009 HFS - Session 2 - Data Collection - Working in Workday" & ".xlsx", objExcel, conn)    'Session 2 test

        'generate_excel_report(objExcel, conn, "Workday_Role_Mapping", "")  'file name, 'where clause

        'generate_error_report(objExcel, conn, "Error_report", "")

        'initiate_unit_reports(objExcel, conn)

        conn.Close()

        objExcel.Quit()

        end_time = Now()

        elapsed_time = DateDiff("s", start_time, end_time)

        Debug.WriteLine("Read " & results(0) & " files in folder in " & elapsed_time / 60 & " minutes.")
        Debug.WriteLine(results(1) & " New files identified.")
        Debug.WriteLine(results(2) & " files successfully added.")
        Debug.WriteLine(results(3) & " files Not added.")
        Debug.WriteLine(results(4) & " files contain format errors.")
        Debug.WriteLine(results(5) & " files contain content errors.")

    End Sub

    Function process_Folder(objExcel, conn) As Integer()

        'results(0) = 'Total number of files read
        'results(1) = 'Total number of new files
        'results(2) = 'Files not added due to format
        'results(3) = 'Files not added due to content

        Dim sSql
        Dim file_count = 0
        Dim folder_count = 0
        Dim rec As ADODB.Recordset
        Dim FileNameWithExt
        Dim folderpath = "\\sharepoint.washington.edu@SSL\DavWWWRoot\oim\proj\HRPayroll\Imp\Supervisory Org Cleanup\Role-Mapping"
        Dim filenames
        Dim results As Integer()
        Dim workbook_results As Integer()
        Dim successful_adds As Integer = 0
        Dim error_format As Integer = 0
        Dim error_content As Integer = 0
        Dim blank_fields As Integer = 0
        Dim blank_fields_academic As Integer = 0
        Dim error_format_string = ""
        Dim error_content_string = ""
        Dim not_added = 0
        Dim debug_state = False

        results = {0, 0, 0, 0, 0, 0}         'Total number of files in folder, Files not already input, files sucessful added, files not added, Files containing format errors, Files containing content errors
        workbook_results = {0, 0, 0, 0, 0}      'successful_reads, add_attempted, not_added, error_format, error_content

        Try
            filenames = My.Computer.FileSystem.GetFiles(folderpath, FileIO.SearchOption.SearchTopLevelOnly)

        Catch ex As System.IO.IOException
            Debug.WriteLine(
            "{0}: The write operation could " &
            "not be performed because the " &
            "specified part of the file is " &
            "locked.", ex.GetType().Name)
            MsgBox("Please ensure that you have access to " & folderpath &
                    "on sharepoint.")

        End Try

        rec = New ADODB.Recordset

        For Each fileName As String In filenames
            'Debug.WriteLine(fileName)
            folder_count = folder_count + 1
            FileNameWithExt = Mid$(fileName, InStrRev(fileName, "\") + 1)
            'Debug.WriteLine(FileNameWithExt)
            sSql = "Select submittalID from submittals where file_name = """ & FileNameWithExt & """"
            'Debug.WriteLine(sSql)
            rec.Open(sSql, conn)

            If (rec.BOF And rec.EOF) Then                                                   'if the file name has not been recorded
                Debug.WriteLine("Processing " & file_count + 1 & ":  " & FileNameWithExt & "...")
                workbook_results = Process_workbook(FileNameWithExt, objExcel, conn)

                If (workbook_results(0) > 0) Then
                    successful_adds = successful_adds + 1
                End If
                If (workbook_results(2) = 1) Then
                    not_added = not_added + 1
                End If

                If (workbook_results(3) = 1) Then
                    error_format = error_format + 1
                End If
                If (workbook_results(4) = 1) Then
                    error_content = error_content + 1
                End If
                file_count = file_count + 1
            End If
            rec.Close()

        Next

        results(0) = folder_count   'Total number of files in folder
        results(1) = file_count     'Files not already input 
        results(2) = successful_adds  'files sucessful added
        results(3) = not_added          'files not added
        results(4) = error_format        'Files containing format errors
        results(5) = error_content       'Files containing content errors


        Return results

        If debug_state = True Then
            Debug.WriteLine("Files in folder: " & results(0))
            Debug.WriteLine("Files not already input: " & results(1))
            Debug.WriteLine("Files successfully added: " & results(2))
            Debug.WriteLine("Files not added: " & results(3))
            Debug.WriteLine("Files containing formatting errors: " & results(4))
            Debug.WriteLine("Files containing content errors: " & results(5))

        End If

        sSql = Nothing
        file_count = Nothing
        folder_count = Nothing
        rec = Nothing
        FileNameWithExt = Nothing
        folderpath = Nothing
        filenames = Nothing
        results = Nothing
        workbook_results = Nothing
        successful_adds = Nothing
        error_format = Nothing
        error_content = Nothing
        blank_fields = Nothing
        blank_fields_academic = Nothing
        error_format_string = Nothing
        error_content_string = Nothing
        not_added = Nothing
        debug_state = Nothing

    End Function

    Function Process_workbook(filename, objExcel, conn) As Integer()

        'workbook_results(0)    = 1 File was inserted 0 file was not inserted
        'workbook_results(1)    = There was an error in format - number of worksheets was not what was expected
        'workbook_results(2)    = There was an error of content - identifying information missing
        'workbook_results(3)    = A count of blank fields
        'workbook_results(4)    = A count of blank fields related to academic Scenarios

        Dim excelPath
        Dim pathToFile
        Dim worksheet
        Dim workbook
        Dim sSql
        Dim unit As String
        Dim contact = ""
        Dim date_submitted As String
        Dim submittalID
        Dim worksheetCount = 0
        Dim error_conditions = 0
        Dim file_ext
        Dim session_no As Integer
        Dim Error_identifying_information = False
        Dim Error_file_type = False
        Dim process_scenario_results As Integer()
        Dim successful_adds = 0
        Dim error_format_ct = 0
        Dim error_content_ct = 0
        Dim error_content_bool = False
        Dim error_format_bool = False
        Dim workbook_results As Integer()
        Dim rec As ADODB.Recordset
        Dim debug_state = False
        Dim add_attempted = 0
        Dim not_added = 0
        Dim unit_map_ID

        workbook_results = {0, 0, 0, 0, 0}      'successful_reads, add_attempted, not_added, error_format, error_content
        process_scenario_results = {0, 0, 0}

        rec = New ADODB.Recordset

        excelPath = "\\sharepoint.washington.edu@SSL\DavWWWRoot\oim\proj\HRPayroll\Imp\Supervisory Org Cleanup\Role-Mapping\"
        pathToFile = excelPath & filename

        file_ext = Mid$(filename, InStrRev(filename, ".") + 1)
        'Debug.WriteLine(file_ext)

        If debug_state = True Then
            Debug.WriteLine("Process_workbook()...")
        End If

        objExcel.Visible = False
        objExcel.DisplayAlerts = 0 ' Don't display any messages about conversion and so forth
        Try
            workbook = objExcel.Workbooks.Open(pathToFile)
        Catch ex As Exception
            Debug.WriteLine(pathToFile & " couldn't be opened.")
        End Try

        If Not IsNothing(workbook) Then
            workbook = objExcel.ActiveWorkbook
            worksheet = workbook.Worksheets(1)

            'Collect Identifying info
            Dim start_row = 4
            Dim start_col = 2
            Do Until worksheet.Cells(start_row, start_col).Value = ""
                If worksheet.Cells(start_row, start_col).Value = "Unit: " _
                    Or worksheet.Cells(start_row, start_col).Value = "Organization for which this was completed" Then
                    start_col = start_col + 1
                Else
                    unit = worksheet.Cells(4, start_col).Value
                    contact = worksheet.Cells(6, start_col).Value
                    date_submitted = worksheet.Cells(8, start_col).Value
                    start_col = start_col + 1
                End If
            Loop

            If IsNothing(unit) Then
                error_content_bool = True
                error_content_ct = error_content_ct + 1
            End If

            session_no = 0

            worksheetCount = workbook.Worksheets.Count

            If worksheetCount > 1 Then
                worksheet = objExcel.ActiveWorkbook.Worksheets(2)
                If Left(worksheet.Name, 2) = "1 " Then                                             'Session 1
                    session_no = 1
                    file_ext = "xlsm"
                ElseIf Left(worksheet.Name, 2) = "5A" Then                           'Session 2
                    session_no = 2
                    'Debug.WriteLine(Left(workbook.Worksheets(4).Name, 2))
                    If Left(workbook.Worksheets(4).Name, 2) = "6A" Then
                        file_ext = "xlsm"
                    Else
                        file_ext = "xlsx"
                    End If
                ElseIf Left(worksheet.Name, 2) = "9A" Then                                          'Session 3
                    session_no = 3
                    If Left(workbook.Worksheets(3).Name, 2) = "9B" Then
                        file_ext = "xlsx"
                    Else
                        file_ext = "xlsm"
                    End If
                End If
            Else
                error_format_bool = True
                error_format_ct = error_format_ct + 1
            End If

            'Debug.WriteLine(file_ext)
            'Debug.WriteLine(session_no)

            If error_content_bool = False And error_format_bool = False And session_no <> 0 Then
                'Debug.WriteLine("Format and content check complete.  processing scenarios")

                'Prepare Unit for verification
                sSql = "SELECT unit_map_ID from unit_correction_map where reported_unit = """ & unit & """"
                'Debug.WriteLine(sSql)
                rec.Open(sSql, conn)

                Dim i = 0
                If (rec.BOF And rec.EOF) Then
                    rec.Close()
                    sSql = "INSERT INTO unit_correction_map (reported_unit) VALUES (""" & unit & """)"
                    'Debug.WriteLine(sSql)
                    conn.Execute(sSql)

                    'Identify the new record's ID
                    sSql = "Select max(unit_map_ID) FROM unit_correction_map"
                    rec.Open(sSql, conn)
                    For Each x In rec.Fields
                        unit_map_ID = x.value
                    Next
                    rec.Close()
                Else
                    For Each x In rec.Fields
                        If Not IsDBNull(x.value) Then
                            unit_map_ID = x.value
                        End If
                    Next
                    rec.Close()
                End If


                'Add a record
                sSql = "INSERT INTO submittals (reported_unit, contact, date_submitted, date_recorded, file_name, session_no, unit_map_ID) VALUES (""" &
                    unit & """, """ & contact & """, """ & date_submitted & """, """ & Format(Now, "MM/dd/yyyy") & """, """ & filename & """,""" & session_no.ToString & """,""" & unit_map_ID & """)"
                'Debug.WriteLine(sSql)
                conn.Execute(sSql)

                'Identify the new record's ID
                sSql = "Select max(submittalID) FROM submittals"
                rec.Open(sSql, conn)
                For Each x In rec.Fields
                    submittalID = x.value
                Next
                rec.Close()

                process_scenario_results = Process_scenarios(objExcel, workbook, conn, submittalID, file_ext, session_no)

                workbook_results(0) = process_scenario_results(0)   'Successful_field_reads
                'workbook_results(3) = process_scenario_results(1)   'blank_field_ct_non-academic
                'workbook_results(4) = process_scenario_results(2)   'blank_field_ct_academic
                add_attempted = add_attempted + 1

                sSql = "DELETE * FROM rejected_submittals WHERE filename = """ & filename & """"
                'Debug.WriteLine(sSql)
                conn.Execute(sSql)

            Else
                Debug.WriteLine("...Format and content check returned errors.  See error log for more information.")
                not_added = not_added + 1

                sSql = "Select rejected_submittalID from rejected_submittals where filename = """ & filename & """"
                'Debug.WriteLine(sSql)
                rec.Open(sSql, conn)

                If (rec.BOF And rec.EOF) Then                                                   'if the file name has not been recorded
                    sSql = "INSERT INTO rejected_submittals (filename, content_error, format_error) values (""" & filename & """, " & error_content_bool & ", " & error_format_bool & ")"
                    Debug.WriteLine(sSql)
                    conn.Execute(sSql)
                End If
                rec.Close()

            End If

            Try
                workbook.Close()
            Catch ex As Exception
                Debug.WriteLine("Could not close workbook.")
            End Try
            workbook = Nothing
            worksheet = Nothing

        End If

        workbook_results(1) = add_attempted
        workbook_results(2) = not_added
        workbook_results(3) = error_format_ct                  'There was an error in format - number of worksheets was not what was expected
        workbook_results(4) = error_content_ct                 'There was an error of content - identifying information missing

        If debug_state = True Then
            Debug.WriteLine("Successful_field_reads: " & workbook_results(0))
            Debug.WriteLine("add_attempted: " & workbook_results(1))
            Debug.WriteLine("not_added: " & workbook_results(2))
            Debug.WriteLine("error_format: " & workbook_results(3))
            Debug.WriteLine("error_content: " & workbook_results(4))
        End If

        'Debug.WriteLine("..." & filename & " processing completed.")

        Return workbook_results

        excelPath = Nothing
        pathToFile = Nothing
        worksheet = Nothing
        workbook = Nothing
        sSql = Nothing
        unit = Nothing
        contact = Nothing
        date_submitted = Nothing
        submittalID = Nothing
        worksheetCount = Nothing
        error_conditions = Nothing
        file_ext = Nothing
        session_no = Nothing
        Error_identifying_information = Nothing
        Error_file_type = Nothing
        process_scenario_results = Nothing
        successful_adds = Nothing
        error_format_ct = Nothing
        error_content_ct = Nothing
        error_content_bool = Nothing
        error_format_bool = Nothing
        workbook_results = Nothing
        rec = Nothing
        debug_state = Nothing
        add_attempted = Nothing
        not_added = Nothing

    End Function

    Function Process_scenarios(objExcel, workbook, conn, submittalID, file_ext, session_no) As Integer()

        'process_scenarios(0) = Count of successful scenarios
        'process_scenarios(1) = Blank Field Count
        'process_scenarios(1) = Blank Field Count Academic


        Dim successful_field_reads = 0
        Dim blank_field_txt = ""
        Dim blank_field_txt_academic = ""
        Dim blank_field_ct = 0
        Dim blank_field_ct_academic = 0
        Dim file_structure_issue = ""
        Dim sSql
        Dim process_scenarios_results As Integer()
        Dim read_field_results As String()
        Dim worksheet_name_error = ""
        Dim worksheet_orient_error = ""
        Dim index = 0
        Dim debug_state = False

        process_scenarios_results = {0, 0, 0}
        read_field_results = {"", "", "", "", "", "", ""} 'Data_found_ct, blank_field_txt, blank_field_ct, blank_field_academic_txt, blank_field_ct_academic, worksheet_name_error, worksheet_orient_error

        If debug_state = True Then
            Debug.WriteLine("Processing Scenarios...")
        End If

        If file_ext = "xlsm" Then                                                                  'non-mac formatted
            If session_no = 1 Then                                                                 'Session 1
                Dim field_definition(0 To 6) As String                              'xlsm session 1
                field_definition(0) = "2,1 W,1,2C,2C,6,3"
                field_definition(1) = "2,1 W,1,3B,3B,12,3"
                field_definition(2) = "3,2 T,2,2A,2A,6,3"
                'Worksheet 4 (3-Time Off) is blank
                field_definition(3) = "5,4A ,4A,2A,2A,6,3"
                field_definition(4) = "6,4B ,4B,2A,2A,6,3"
                field_definition(5) = "6,4B ,4B,3A,3A,12,3"
                field_definition(6) = "7,4C ,4C,3A,3A,6,3"
                read_field_results = read_field(field_definition, workbook, conn, submittalID)
            ElseIf session_no = 2 Then                                                             'Session 2
                Dim field_definition(0 To 20) As String                             'xlsm session 2
                field_definition(0) = "2,5A ,5A,2B,2B,5,3"
                field_definition(1) = "2,5A ,5A,4A,4A,11,3"
                field_definition(2) = "3,5B ,5B,2B,2B,5,3"
                field_definition(3) = "3,5B ,5B,3A,3A,11,3"
                field_definition(4) = "3,5B ,5B,4A,4A,17,3"
                field_definition(5) = "4,6A ,6A,2A,2A,5,3"
                field_definition(6) = "4,6A ,6A,3A,3A,11,3"
                field_definition(7) = "5,6B ,6B,1A,1A,5,3"
                field_definition(8) = "5,6B ,6B,2A,2A,11,3"
                field_definition(9) = "5,6B ,6B,3A,3A,17,3"
                field_definition(10) = "6,7 O,7,4A,4A,5,3"
                field_definition(11) = "7,8A ,8A,1A,1A,5,3"
                field_definition(12) = "7,8A ,8A,2A,2A,11,3"
                field_definition(13) = "7,8A ,8A,2B,2B,17,3"
                field_definition(14) = "7,8A ,8A,3A,3A,23,3"
                field_definition(15) = "8,8B ,8B,1B,1B,5,3"
                field_definition(16) = "8,8B ,8B,2A,2A,11,3"
                field_definition(17) = "8,8B ,8B,3A,3A,17,3"
                field_definition(18) = "8,8B ,8B,4A,4A,23,3"
                field_definition(19) = "9,8C ,8C,2A,2A,5,3"
                field_definition(20) = "9,8C ,8C,3A,3A,11,3"
                read_field_results = read_field(field_definition, workbook, conn, submittalID)
            ElseIf session_no = 3 Then                                                             'Session 3
                Dim field_definition(0 To 14) As String                             'xlsm session 3
                field_definition(0) = "2,9A ,9A,2A,2A,4,3"
                field_definition(1) = "2,9A ,9A,3A,3A,10,3"
                'worksheet 3 ("3 Time Off") is blank
                field_definition(2) = "4,9B ,9B,2B,2B,4,3"
                field_definition(3) = "5,10 ,10,2A,2A,4,3"
                field_definition(4) = "5,10 ,10,3A,3A,10,3"
                field_definition(5) = "6,11A,11A,2A,2A,4,3"
                field_definition(6) = "6,11A,11A,3A,3A,10,3"
                field_definition(7) = "6,11A,11A,3B,3B,16,3"
                field_definition(8) = "6,11A,11A,4A,4A,22,3"
                field_definition(9) = "7,11B,11B,2A,2A,4,3"
                field_definition(10) = "7,11B,11B,3A,3A,10,3"
                field_definition(11) = "7,11B,11B,3B,3B,16,3"
                field_definition(12) = "8,12 ,12,2A,2A,4,3"
                field_definition(13) = "8,12 ,12,3A,3A,10,3"
                field_definition(14) = "8,12 ,12,4A,4A,16,3"
                read_field_results = read_field(field_definition, workbook, conn, submittalID)
            ElseIf session_no = 0 Then
                file_structure_issue = "x"
            End If
        ElseIf file_ext = "xlsx" Then
            If session_no = 1 Then                                                                 'Session 1
                Dim field_definition(0 To 6) As String                          'xlsx session 1
                field_definition(0) = "2,1 W,1,2C,2C,6,3"
                field_definition(1) = "2,1 W,1,3B,3B,12,3"
                field_definition(2) = "3,2 T,2,2A,2A,6,3"
                'Worksheet 4 (3-Time Off) is blank
                field_definition(3) = "5,4A ,4A,2A,2A,6,3"
                field_definition(4) = "6,4B ,4B,2A,2A,6,3"
                field_definition(5) = "6,4B ,4B,3A,3A,12,3"
                field_definition(6) = "7,4C ,4C,3A,3A,6,3"
                read_field_results = read_field(field_definition, workbook, conn, submittalID)
            ElseIf session_no = 2 Then                                                             'Session 2
                Dim field_definition(0 To 20) As String                         'xlsx session 2
                field_definition(0) = "2,5A ,5A,2B,2B,5,3"
                field_definition(1) = "2,5A ,5A,4A,4A,11,3"

                field_definition(2) = "3,5B ,5B,2B,2B,6,3"
                field_definition(3) = "3,5B ,5B,3A,3A,12,3"
                field_definition(4) = "3,5B ,5B,4A,4A,18,3"

                'worksheet 4 contains time off information,is blank

                field_definition(5) = "5,6B ,6A,2A,2A,6,3"    'Typo on tab of data collection tool
                field_definition(6) = "5,6B ,6A,3A,3A,12,3"   'Typo on tab of data collection tool

                field_definition(7) = "6,6B ,6B,1A,1A,6,3"
                field_definition(8) = "6,6B ,6B,2A,2A,12,3"
                field_definition(9) = "6,6B ,6B,3A,3A,18,3"

                field_definition(10) = "7,7 O,7,4A,3A,6,3"

                field_definition(11) = "8,8A ,8A,1A,1A,6,3"
                field_definition(12) = "8,8A ,8A,2A,2A,12,3"
                field_definition(13) = "8,8A ,8A,2B,2B,18,3"
                field_definition(14) = "8,8A ,8A,3A,3A,24,3"

                field_definition(15) = "9,8B ,8B,1B,1B,6,3"
                field_definition(16) = "9,8B ,8B,2A,2A,12,3"
                field_definition(17) = "9,8B ,8B,3A,3A,18,3"
                field_definition(18) = "9,8B ,8B,4A,4A,24,3"

                field_definition(19) = "9,8C ,8C,2A,2A,5,3"
                field_definition(20) = "9,8C ,8C,3A,3A,11,3"
                read_field_results = read_field(field_definition, workbook, conn, submittalID)
            ElseIf session_no = 3 Then                                                             'Session 3
                Dim field_definition(0 To 14) As String                      'xlsx Session 3
                field_definition(0) = "2,9A ,9A,2A,2A,5,3"
                field_definition(1) = "2,9A ,9A,3A,3A,11,3"

                field_definition(2) = "3,9B ,9B,2B,2A,6,3"

                'worksheet 5 ("3 Time off") is blank

                field_definition(3) = "3,10 ,10,2A,2A,6,3"
                field_definition(4) = "3,10 ,10,3A,3A,12,3"

                field_definition(5) = "6,11A,11A,2A,2A,6,3"
                field_definition(6) = "6,11A,11A,3A,3A,12,3"
                field_definition(7) = "6,11A,11A,3B,3B,18,3"
                field_definition(8) = "6,11A,11A,4A,4A,24,3"

                field_definition(9) = "7,11B,11B,2A,2A,6,3"
                field_definition(10) = "7,11B,11B,3A,3A,12,3"
                field_definition(11) = "7,11B,11B,3B,3B,18,3"

                field_definition(12) = "8,12 ,12,2A,2A,6,3"
                field_definition(13) = "8,12 ,12,3A,3A,12,3"
                field_definition(14) = "8,12 ,12,4A,4A,18,3"
                read_field_results = read_field(field_definition, workbook, conn, submittalID)
            ElseIf session_no = 0 Then
                file_structure_issue = "x"
            End If
        Else
            'Debug.WriteLine("The file was either Not an xlsm Or xlsx.")
        End If
        objExcel.ActiveWorkbook.Close(SaveChanges:=False)

        successful_field_reads = CInt(read_field_results(0))
        blank_field_txt = read_field_results(1)
        blank_field_ct = CInt(read_field_results(2))
        blank_field_txt_academic = read_field_results(3)
        blank_field_ct_academic = CInt(read_field_results(4))
        worksheet_name_error = read_field_results(5)
        worksheet_orient_error = read_field_results(6)



        If debug_state = True Then
            Debug.WriteLine("Sucessful Reads:" & successful_field_reads)
            Debug.WriteLine("blank_field_txt:" & blank_field_txt)
            Debug.WriteLine("blank_field_ct:" & blank_field_ct)
            Debug.WriteLine("blank_field_txt_academic:" & blank_field_txt_academic)
            Debug.WriteLine("blank_field_ct_academic: " & blank_field_ct_academic)
            Debug.WriteLine("worksheet_name_error:" & worksheet_name_error)
            Debug.WriteLine("worksheet_orient_error: " & worksheet_orient_error)
        End If

        If blank_field_ct > 0 Then
            sSql = "UPDATE submittals SET blank_fields_non_academic = """ & blank_field_txt & """ WHERE submittalID = " & submittalID
            'Debug.WriteLine(sSql)
            conn.Execute(sSql)

        End If

        If blank_field_ct_academic > 0 Then
            sSql = "UPDATE submittals SET blank_fields_academic = """ & blank_field_txt_academic & """ WHERE submittalID = " & submittalID
            'Debug.WriteLine(sSql)
            conn.Execute(sSql)

        End If

        If worksheet_name_error <> "Worksheet name errors: (expected):(encountered);" Then
            sSql = "UPDATE submittals SET worksheet_name_error = """ & worksheet_name_error & """ WHERE submittalID = " & submittalID
            'Debug.WriteLine(sSql)
            conn.Execute(sSql)
        End If

        If worksheet_orient_error <> "Orient cell errors: (s):(f):(oc);" Then
            sSql = "UPDATE submittals SET worksheet_orient_error = """ & worksheet_orient_error & """ WHERE submittalID = " & submittalID
            'Debug.WriteLine(sSql)
            conn.Execute(sSql)
        End If


        process_scenarios_results(0) = successful_field_reads
        process_scenarios_results(1) = blank_field_ct
        process_scenarios_results(2) = blank_field_ct_academic

        Return process_scenarios_results

        successful_field_reads = Nothing
        blank_field_txt = Nothing
        blank_field_txt_academic = Nothing
        blank_field_ct = Nothing
        blank_field_ct_academic = Nothing
        file_structure_issue = Nothing
        sSql = Nothing
        process_scenarios_results = Nothing
        read_field_results = Nothing
        worksheet_name_error = Nothing
        worksheet_orient_error = Nothing
        index = Nothing
        debug_state = Nothing

    End Function

    Function read_field(field_definition, workbook, conn, submittalID) As String()

        'Returns a file field string array

        'read_field_results(0) = data_found_ct.ToString             'The number of field entries found
        'read_field_results(1) = blank_field_txt                    'A string of non-academic blank fields
        'read_field_results(2) = blank_field_ct.ToString            'a count of non-academic fields
        'read_field_results(3) = blank_field_txt_academic           'a string of academic blank fields
        'read_field_results(4) = blank_field_ct_academic.ToString   'a count of academic blank fields

        Dim foo
        Dim index = 0
        Dim worksheet
        Dim worksheetName
        Dim scenario
        Dim orient_cell
        Dim startRow
        Dim startCol
        Dim collect_field_results As String()
        Dim blank_field_txt_academic = ""
        Dim blank_field_txt = ""
        Dim blank_field_ct = 0
        Dim blank_field_ct_academic = 0
        Dim worksheet_name_error = "Worksheet name errors: (expected):(encountered);"
        Dim worksheet_orient_error = "Orient cell errors: (s):(f):(oc);"
        Dim read_field_results As String()
        Dim data_found_ct = 0
        Dim debug_state = True

        read_field_results = {"", "", "", "", "", "", ""} 'Data_found_ct, blank_field_txt, blank_field_ct, blank_field_academic_txt, blank_field_ct_academic, worksheet_name_error, worksheet_orient_error
        collect_field_results = {"", "", ""}

        If debug_state = True Then
            Debug.WriteLine("Reading fields for submittalID " & submittalID)
        End If

        For Each field In field_definition
            foo = Split(field, ",")
            worksheet = CInt(foo(0))
            worksheetName = foo(1)
            scenario = foo(2)
            field = foo(3)
            orient_cell = foo(4)
            startRow = CInt(foo(5))
            startCol = CInt(foo(6))
            collect_field_results = collect_field(workbook, conn, submittalID, worksheet, worksheetName, scenario, field, orient_cell, startRow, startCol)

            If CInt(collect_field_results(0)) = 0 Then
                If scenario = "4C" _
                    Or scenario = "5B" _
                       Or scenario = "6B" _
                       Or scenario = "8B" _
                       Or scenario = "11A" _
                       Or scenario = "11B" Then
                    blank_field_txt_academic = blank_field_txt_academic & " " & foo(2) & ":" & foo(3) & ";"
                    blank_field_ct_academic = blank_field_ct_academic + 1
                Else
                    blank_field_txt = blank_field_txt & " " & foo(2) & ":" & foo(3) & ";"
                    blank_field_ct = blank_field_ct + 1
                End If


            Else
                data_found_ct = data_found_ct + CInt(collect_field_results(0))
            End If

            worksheet_name_error = worksheet_name_error & collect_field_results(1)
            worksheet_orient_error = worksheet_orient_error & collect_field_results(1)

            index = index + 1
        Next

        read_field_results(0) = data_found_ct.ToString
        read_field_results(1) = blank_field_txt
        read_field_results(2) = blank_field_ct.ToString
        read_field_results(3) = blank_field_txt_academic
        read_field_results(4) = blank_field_ct_academic.ToString
        read_field_results(5) = worksheet_name_error
        read_field_results(6) = worksheet_orient_error

        If debug_state = True Then
            Debug.WriteLine("Read Field: Cound of Data Found: " & read_field_results(0))
            Debug.WriteLine("Read Field: Blank_field_txt: " & read_field_results(1))
            Debug.WriteLine("Read Field: Blank_field_ct: " & read_field_results(2))
            Debug.WriteLine("Read Field: blank_field_academic: " & read_field_results(3))
            Debug.WriteLine("Read Field: blank_field_ct_academic: " & read_field_results(4))
            Debug.WriteLine("Read Field: worksheet name error: " & read_field_results(5))
            Debug.WriteLine("Read Field: worksheet orient error: " & read_field_results(6))
        End If

        Return read_field_results

        foo = Nothing
        index = Nothing
        worksheet = Nothing
        worksheetName = Nothing
        scenario = Nothing
        orient_cell = Nothing
        startRow = Nothing
        startCol = Nothing
        collect_field_results = Nothing
        blank_field_txt_academic = Nothing
        blank_field_txt = Nothing
        blank_field_ct = Nothing
        blank_field_ct_academic = Nothing
        worksheet_name_error = Nothing
        worksheet_orient_error = Nothing
        read_field_results = Nothing
        data_found_ct = Nothing
        debug_state = Nothing

    End Function

    Private Function collect_field(workbook, conn, submittalID, worksheet, worksheetName, scenario, field, orient_cell, startRow, startCol) As String()

        'Returns the number of field entries encountered.  Blank if 0

        Dim curRow
        Dim curCol
        Dim currentWorkSheet
        Dim first_name
        Dim last_name
        Dim eid
        Dim org_team As String
        Dim budget_no
        Dim sSql
        Dim rec As ADODB.Recordset
        Dim responseID As Integer
        Dim org_team_mapID
        Dim index
        Dim debug_state = True
        Dim worksheet_name_error = ""
        Dim worksheet_orient_error = ""
        Dim results = {"", "", ""}   'index, worksheet_name_error, worksheet_orient_error
        Dim r
        Dim i

        index = 0
        org_team_mapID = 0

        rec = New ADODB.Recordset

        Try
            currentWorkSheet = workbook.Worksheets(worksheet)
        Catch ex As Exception
            Debug.WriteLine("worksheet " & worksheet & "Not found.")
        End Try

        'currentWorkSheet = workbook.Worksheets(worksheetName)        'Breaks if people modify the table names. 

        'Debug.WriteLine("Reading " & scenario & ":" & field & " data from worksheet " & currentWorkSheet.Name)

        If Not IsNothing(currentWorkSheet) Then
            'Debug.WriteLine(Left(currentWorkSheet.name, 3) & " " & worksheetName)
            If Left(currentWorkSheet.name, 3) = worksheetName Then
                r = currentWorkSheet.Cells.Find(What:=orient_cell)
                If Not IsNothing(r) Then

                    'Debug.WriteLine("Column: " & r.column)
                    'Debug.WriteLine("Row: " & r.row)
                    'startRow = startRow
                    'startCol = startCol
                    startRow = r.row + 1
                    startCol = r.column

                    curRow = startRow
                    curCol = startCol
                    'Debug.WriteLine( "Start RC " & startRow &","& startCol
                    'Debug.WriteLine( "Current RC " & curRow &", "& curCol

                    Do Until currentWorkSheet.Cells(curRow, curCol).Value = ""
                        If currentWorkSheet.Cells(curRow, curCol).Value = "Ex: Elizabeth" _
                            Or currentWorkSheet.Cells(curRow, curCol).Value = "EXAMPLE: Peter" _
                            Or currentWorkSheet.Cells(curRow, curCol).Value = "EXAMPLE: Smith" _
                            Or currentWorkSheet.Cells(curRow, curCol).Value = "N/A" _
                            Or currentWorkSheet.Cells(curRow, curCol).Value = "n/a" _
                            Or currentWorkSheet.Cells(curRow, curCol).Value = "First Name(s)" Then
                            curCol = curCol + 1
                        Else
                            first_name = Trim(currentWorkSheet.Cells(curRow, curCol).Value)
                            curRow = curRow + 1
                            'Debug.WriteLine(first_name)
                            last_name = Trim(currentWorkSheet.Cells(curRow, curCol).Value)
                            curRow = curRow + 1
                            'Debug.WriteLine(last_name)
                            eid = Trim(currentWorkSheet.Cells(curRow, curCol).Value)
                            If Not IsNothing(eid) Then
                                eid = eid.ToString()
                                eid = eid.replace("-", "")
                            End If
                            'Debug.WriteLine(eid)
                            curRow = curRow + 1
                            'Org Team
                            org_team = Trim(currentWorkSheet.Cells(curRow, curCol).Value)
                            'Debug.WriteLine(org_team)

                            sSql = "SELECT org_team_mapID from org_team_map where org_team = """ & org_team & """"
                            'Debug.WriteLine(sSql)
                            rec.Open(sSql, conn)

                            If (rec.BOF And rec.EOF) Then
                                sSql = "INSERT INTO org_team_map (org_team) VALUES (""" & org_team & """)"
                                'Debug.WriteLine(sSql)
                                conn.Execute(sSql)
                            End If

                            rec.Close()

                            sSql = "SELECT org_team_mapID from org_team_map where org_team = """ & org_team & """"
                            'Debug.WriteLine(sSql)
                            rec.Open(sSql, conn)

                            If (rec.BOF And rec.EOF) Then
                                org_team_mapID = 0
                            Else
                                For Each x In rec.Fields
                                    If Not IsDBNull(x.value) Then
                                        org_team_mapID = x.value
                                    End If
                                Next
                            End If

                            rec.Close()
                            curRow = curRow + 1

                            budget_no = currentWorkSheet.Cells(curRow, curCol).Value
                            curRow = curRow + 1

                            sSql = "SELECT max(responseID) from responses where org_team = """ & org_team & """ AND EID = """ & eid & """" & " AND submittalID = " & submittalID
                            'Debug.WriteLine(sSql)
                            rec.Open(sSql, conn)

                            i = 0
                            responseID = 0
                            If (rec.BOF And rec.EOF) Then
                                'Debug.WriteLine("The line Is empty")
                            Else
                                For Each x In rec.Fields
                                    If Not IsDBNull(x.value) Then

                                        responseID = x.value
                                    End If
                                Next
                            End If

                            rec.Close()

                            'Debug.WriteLine(responseID)

                            If responseID > 0 Then
                                sSql = "UPDATE responses Set " & scenario & "_" & field & " = 'x' WHERE responseID = " & responseID
                            Else
                                sSql = "INSERT INTO responses (first_name, last_name, EID, org_team_mapID, org_team, budget_no, " & scenario & "_" & field & ", date_recorded, submittalID) values (""" &
                                        first_name & """, """ & last_name & """, """ & eid & """, " & org_team_mapID & ", """ & org_team & """, """ & budget_no & """, ""x"",""" & Format(Now, "MM/dd/yyyy") & """, " & submittalID & ")"
                            End If
                            'Debug.WriteLine(sSql)
                            conn.Execute(sSql)
                            curRow = startRow
                            curCol = curCol + 1
                            index = index + 1
                            'Debug.WriteLine( "RC: " & curRow &","& curCol
                        End If
                    Loop
                Else
                    'Debug.WriteLine("Orient Cell  " & orient_cell & " not encountered.")
                    worksheet_orient_error = " " & scenario & ":" & field & ":" & orient_cell & ";"
                End If
            Else
                worksheet_name_error = " " & worksheetName & ":" & Left(currentWorkSheet.Name, 3) & ";"
                'Debug.WriteLine("Sheetname starting With " & worksheetName & " expected, found worksheet starting With " & Left(currentWorkSheet.Name, 2) & ".")
            End If
        End If

        If index = 0 Then
            'Debug.WriteLine("The program found no records For the " & scenario & ":" & field & " field.")
        End If

        Try
            currentWorkSheet.close()
        Catch ex As Exception
            'Debug.WriteLine("Couldn't Close worksheet")
        End Try

        results(0) = index
        results(1) = worksheet_name_error
        results(2) = worksheet_orient_error

        Return results

        curRow = Nothing
        curCol = Nothing
        currentWorkSheet = Nothing
        first_name = Nothing
        last_name = Nothing
        eid = Nothing
        org_team = Nothing
        budget_no = Nothing
        sSql = Nothing
        rec = Nothing
        responseID = Nothing
        org_team_mapID = Nothing
        index = Nothing
        debug_state = Nothing
        worksheet_name_error = Nothing
        worksheet_orient_error = Nothing
        results = Nothing
        r = Nothing
        i = Nothing

    End Function

    Function TransposeDim(v As Object) As Object
        ' Custom Function to Transpose a 0-based array (v)

        Dim X As Long, Y As Long, Xupper As Long, Yupper As Long
        Dim tempArray As Object

        Xupper = UBound(v, 2)
        Yupper = UBound(v, 1)

        ReDim tempArray(Xupper, Yupper)
        For X = 0 To Xupper
            For Y = 0 To Yupper
                tempArray(X, Y) = v(Y, X)
            Next Y
        Next X

        TransposeDim = tempArray
    End Function

    Function generate_excel_report(objExcel, conn, file_name, where_clause)
        Dim file_path = ""
        Dim folder = "C:\submissions\"
        Dim file_ext = ".xlsx"
        Dim workbook
        Dim worksheet
        Dim record_count = 1960

        file_path = folder & file_name & file_ext

        Debug.WriteLine("Generating " & file_name & file_ext & "...")

        objExcel.Visible = False
        objExcel.DisplayAlerts = 0 ' Don't display any messages about conversion and so forth
        workbook = objExcel.Workbooks.Add
        workbook.Sheets.Add
        workbook.Sheets.Add
        workbook.Sheets.Add
        worksheet = workbook.Worksheets("Sheet4")
        worksheet.Name = "Roles"
        worksheet = workbook.Worksheets("Sheet3")
        worksheet.Name = "Field by Role"
        worksheet = workbook.Worksheets("Sheet2")
        worksheet.Name = "Field by Scenario"
        worksheet = workbook.Worksheets("Sheet1")
        worksheet.Name = "Role Confirmation Tool"
        workbook.SaveAs(FileName:=file_path)
        workbook.Close()

        generate_field_by_role_report(objExcel, conn, where_clause, file_path, "Field by Role", record_count)
        generate_field_by_scenario_report(objExcel, conn, where_clause, file_path, "Field by Scenario", record_count)
        generate_by_role_report(objExcel, conn, where_clause, file_path, "Roles", record_count)

        workbook = Nothing
        worksheet = Nothing
        folder = Nothing
        file_ext = Nothing
        file_path = Nothing

    End Function

    Function initiate_unit_reports(objExcel, conn)
        Dim sSql
        Dim rec As ADODB.Recordset
        Dim Unit = ""
        Dim record_count = 0
        Dim file_name = ""
        Dim where_clause = ""
        Dim i

        rec = New ADODB.Recordset

        sSql = "SELECT * FROM Workday_Role_mapping_summary"
        Debug.WriteLine(sSql)
        rec.Open(sSql, conn)

        Debug.WriteLine("   Generating Unit reports...")

        Try
            MkDir("C:\submissions\Unit Reports\")
        Catch ex As Exception
            'Debug.WriteLine("Folder already Exists")
        End Try

        Dim j = 0
        If (rec.BOF And rec.EOF) Then
            Debug.WriteLine("No records found.")
        Else
            Do While Not rec.EOF
                Dim fld
                i = 0
                Dim start_time = Now()
                For Each fld In rec.Fields 'fails here, hilighting fld
                    If i = 0 Then
                        Unit = fld.value
                    Else
                        record_count = CInt(fld.value)
                    End If
                    i = i + 1
                Next fld
                'Debug.WriteLine("Record Count: " & record_count & "; Unit: " & Unit)
                file_name = "Working_in_Workday_" & Unit
                where_clause = Unit
                generate_unit_report(objExcel, conn, file_name, where_clause, record_count)
                Dim end_time = Now()
                Dim elapsed_time = DateDiff("s", start_time, end_time)
                Debug.WriteLine("Processed " & j & ": " & Unit & " in " & elapsed_time & " seconds")
                rec.MoveNext()
                i = 0
                j = j + 1
            Loop
        End If
        rec.Close()

    End Function

    Function generate_unit_report(objExcel, conn, file_name, where_clause, record_count)
        Dim file_path = ""
        Dim folder = "C:\submissions\Unit Reports\"
        Dim file_ext = ".xlsx"
        Dim workbook
        Dim worksheet

        file_path = folder & file_name & file_ext

        objExcel.Visible = False
        objExcel.DisplayAlerts = 0 ' Don't display any messages about conversion and so forth
        workbook = objExcel.Workbooks.Add
        workbook.Sheets.Add
        workbook.Sheets.Add
        workbook.Sheets.Add
        worksheet = workbook.Worksheets("Sheet4")
        worksheet.Name = "Roles"
        worksheet = workbook.Worksheets("Sheet3")
        worksheet.Name = "Field by Role"
        worksheet = workbook.Worksheets("Sheet2")
        worksheet.Name = "Field by Scenario"
        worksheet = workbook.Worksheets("Sheet1")
        worksheet.Name = "Role Confirmation Tool"
        workbook.SaveAs(FileName:=file_path)
        workbook.Close()

        generate_field_by_role_report(objExcel, conn, " WHERE unit = """ & where_clause & """", file_path, "Field by Role", record_count)
        generate_field_by_scenario_report(objExcel, conn, " WHERE unit = """ & where_clause & """", file_path, "Field by Scenario", record_count)
        generate_by_role_report(objExcel, conn, "WHERE unit = """ & where_clause & """", file_path, "Roles", record_count)
        generate_role_confirmation_tool(objExcel, conn, "WHERE unit = """ & where_clause & """", file_path, "Role Confirmation Tool", record_count)

        workbook = Nothing
        worksheet = Nothing
        folder = Nothing
        file_ext = Nothing
        file_path = Nothing

    End Function

    Function generate_error_report(objExcel, conn, file_name, where_clause)
        Dim file_path = ""
        Dim folder = "C: \submissions\"
        Dim file_ext = ".xlsx"
        Dim workbook
        Dim worksheet

        file_path = folder & file_name & file_ext

        Debug.WriteLine("Generating " & file_name & file_ext & "...")

        objExcel.Visible = False
        objExcel.DisplayAlerts = 0 ' Don't display any messages about conversion and so forth
        workbook = objExcel.Workbooks.Add
        ' default sheet       'Identifying information not as expected
        workbook.Sheets.Add   'Couldn't identify session
        workbook.Sheets.Add   'Blank EIDs
        workbook.Sheets.Add   'Malformed EIDs
        workbook.Sheets.Add   'Unexpected Tab Name
        workbook.Sheets.Add   'Can't center cursor on Start Row, Start Column

        Dim Report_definition(0 To 5) As String
        Report_definition(0) = "Sheet6,Missing identifying information,Select * from Errors_no_identifying_information"
        Report_definition(1) = "Sheet5,Session Not identified,Select * from Errors_session_not_identified"
        Report_definition(2) = "Sheet4,Blank EIDs,Select * from Errors_EID_blank"
        Report_definition(3) = "Sheet3,Malformed EIDs,Select * from Errors_EID_malformed"
        Report_definition(4) = "Sheet2,Unexpected tab name,Select * from Errors_worksheet_name"
        Report_definition(5) = "Sheet1,Orient Cell Not encountered,Select * from Errors_worksheet_orient"

        For Each report In Report_definition
            Dim foo = Split(report, ",")
            Dim sheet_name = foo(0)
            Dim new_sheet_name = foo(1)
            Dim sSql = foo(2)
            worksheet = workbook.Worksheets(sheet_name)
            worksheet.Name = new_sheet_name
        Next

        workbook.SaveAs(FileName:=file_path)
        workbook.Close()

        For Each report In Report_definition
            Dim foo = Split(report, ",")
            Dim sheet_name = foo(0)
            Dim new_sheet_name = foo(1)
            Dim sSql = foo(2)
            generate_generic_report(objExcel, conn, sSql, file_path, new_sheet_name)
        Next

        workbook = Nothing
        worksheet = Nothing
        folder = Nothing
        file_ext = Nothing
        file_path = Nothing

    End Function

    Function generate_by_role_report(objExcel, conn, where_clause, file_path, worksheet_name, record_count)
        Dim sSql
        Dim rec As ADODB.Recordset
        Dim workbook
        Dim worksheet

        rec = New ADODB.Recordset
        sSql = "SELECT * FROM Workday_Role_Mapping_by_role " & where_clause
        'Debug.WriteLine(sSql)
        rec.Open(sSql, conn)
        generate_worksheet(objExcel, rec, file_path, worksheet_name)
        rec.Close()

        workbook = objExcel.Workbooks.Open(file_path)
        worksheet = workbook.Worksheets(worksheet_name)

        Dim max_column_txt = worksheet.Cells(1, 49).Address
        Dim max_cell_txt = worksheet.Cells(record_count + 3, 49).Address

        worksheet.Columns("A:A").ColumnWidth = 40
        worksheet.Columns("B:B").ColumnWidth = 8
        worksheet.Columns("C:C").ColumnWidth = 15
        worksheet.Columns("D:D").ColumnWidth = 15
        worksheet.Columns("E:E").ColumnWidth = 10
        worksheet.Columns("F:F").ColumnWidth = 50
        worksheet.Columns("G:G").ColumnWidth = 6
        worksheet.Columns("H:H").ColumnWidth = 6
        worksheet.Columns("I:I").ColumnWidth = 6
        worksheet.Columns("J:J").ColumnWidth = 6
        worksheet.Columns("K:K").ColumnWidth = 6
        worksheet.Columns("L:L").ColumnWidth = 6
        worksheet.Columns("M:M").ColumnWidth = 6
        worksheet.Columns("N:N").ColumnWidth = 6

        worksheet.Rows("1").Insert
        'worksheet.Range("G2:N2").Cut

        worksheet.Cells(1, 7).Value = "I9"
        worksheet.Cells(1, 8).Value = "ABP"
        worksheet.Cells(1, 9).Value = "ACP"
        worksheet.Cells(1, 10).Value = "CP"
        worksheet.Cells(1, 11).Value = "CAC"
        worksheet.Cells(1, 12).Value = "HRC"
        worksheet.Cells(1, 13).Value = "HRP"
        worksheet.Cells(1, 14).Value = "TC"

        worksheet.Cells(2, 7).Value = "I-9 Coordinator"
        worksheet.Cells(2, 8).Value = "Absence Partner"
        worksheet.Cells(2, 9).Value = "Academic Partner"
        worksheet.Cells(2, 10).Value = "Compensation Partner"
        worksheet.Cells(2, 11).Value = "Costing Allocation Partner"
        worksheet.Cells(2, 12).Value = "Human Resource Coordinator"
        worksheet.Cells(2, 13).Value = "Human Resource Partner"
        worksheet.Cells(2, 14).Value = "Time Coordinator"

        worksheet.Columns("G:G").Interior.Color = RGB(253, 228, 207)
        worksheet.Columns("H:H").Interior.Color = RGB(218, 231, 246)
        worksheet.Columns("I:I").Interior.Color = RGB(246, 230, 230)
        worksheet.Columns("J:J").Interior.Color = RGB(238, 234, 242)
        worksheet.Columns("K:K").Interior.Color = RGB(228, 223, 236)
        worksheet.Columns("L:L").Interior.Color = RGB(228, 228, 228)
        worksheet.Columns("M:M").Interior.Color = RGB(205, 233, 239)
        worksheet.Columns("N:N").Interior.Color = RGB(241, 245, 231)

        worksheet.Range("G1:G2").Interior.Color = RGB(247, 150, 70)
        worksheet.Range("H1:H2").Interior.Color = RGB(83, 141, 213)
        worksheet.Range("I1:I2").Interior.Color = RGB(218, 150, 148)
        worksheet.Range("J1:J2").Interior.Color = RGB(128, 100, 162)
        worksheet.Range("K1:K2").Interior.Color = RGB(228, 223, 236)
        worksheet.Range("L1:L2").Interior.Color = RGB(178, 178, 178)
        worksheet.Range("M1:M2").Interior.Color = RGB(49, 134, 155)
        worksheet.Range("N1:N2").Interior.Color = RGB(196, 215, 155)

        worksheet.Range("A1:N2").Font.Bold = True

        worksheet.Range("A2:N2").Borders(Excel.XlBordersIndex.xlEdgeBottom).LineStyle = Excel.XlLineStyle.xlContinuous

        worksheet.Range("A3:" & max_cell_txt).Borders(Excel.XlBordersIndex.xlInsideHorizontal).LineStyle = Excel.XlLineStyle.xlDot
        worksheet.Range("A3:" & max_cell_txt).Borders(Excel.XlBordersIndex.xlInsideHorizontal).ThemeColor = 1
        worksheet.Range("A3:" & max_cell_txt).Borders(Excel.XlBordersIndex.xlInsideHorizontal).TintAndShade = -0.14996795556505

        worksheet.Columns("G:N").HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter

        worksheet.Range("G2:N2").Orientation = 90

        worksheet.Columns("A:A").WrapText = True
        worksheet.Columns("F:F").WrapText = True

        worksheet.Range("A2:" & max_cell_txt).Autofilter

        worksheet.PageSetup.PrintArea = "$A$1:" & max_cell_txt
        worksheet.PageSetup.Orientation = Excel.XlPageOrientation.xlLandscape
        worksheet.PageSetup.PaperSize = Excel.XlPaperSize.xlPaper11x17
        worksheet.PageSetup.PrintTitleRows = "$1:$2"
        worksheet.PageSetup.PrintTitleColumns = "$A:$F"
        worksheet.PageSetup.CenterHeader = where_clause & Chr(10) & worksheet_name
        worksheet.PageSetup.RightHeader = "&D"

        workbook.Save()
        workbook.Close

        sSql = Nothing
        rec = Nothing
        workbook = Nothing
        worksheet = Nothing

    End Function

    Function generate_field_by_role_report(objExcel, conn, where_clause, file_path, worksheet_name, record_count)
        Dim sSql
        Dim rec As ADODB.Recordset
        Dim index As Integer
        Dim workbook
        Dim worksheet

        rec = New ADODB.Recordset
        sSql = "SELECT * FROM Workday_Role_Mapping_by_field_in_order_of_role" & where_clause
        'Debug.WriteLine(sSql)
        rec.Open(sSql, conn)
        generate_worksheet(objExcel, rec, file_path, worksheet_name)
        rec.Close()

        workbook = objExcel.Workbooks.Open(file_path)
        objExcel.Visible = False
        worksheet = workbook.Worksheets(worksheet_name)

        Dim max_column_txt = worksheet.Cells(1, 49).Address
        Dim max_cell_txt = worksheet.Cells(record_count + 3, 49).Address

        worksheet.Columns("A:A").ColumnWidth = 40
        worksheet.Columns("B:B").ColumnWidth = 8
        worksheet.Columns("C:C").ColumnWidth = 15
        worksheet.Columns("D:D").ColumnWidth = 15
        worksheet.Columns("E:E").ColumnWidth = 10
        worksheet.Columns("F:F").ColumnWidth = 50
        worksheet.Columns("G:AW").ColumnWidth = 3

        worksheet.Rows("1").Insert
        worksheet.Rows("1").Insert

        worksheet.Cells(1, 7).Value = "I9"
        worksheet.Cells(1, 8).Value = "ABP"
        worksheet.Cells(1, 9).Value = "ABP"
        worksheet.Cells(1, 10).Value = "ABP"
        worksheet.Cells(1, 11).Value = "ABP"
        worksheet.Cells(1, 12).Value = "ABP"
        worksheet.Cells(1, 13).Value = "ACP"
        worksheet.Cells(1, 14).Value = "ACP"
        worksheet.Cells(1, 15).Value = "ACP"
        worksheet.Cells(1, 16).Value = "ACP"
        worksheet.Cells(1, 17).Value = "ACP"
        worksheet.Cells(1, 18).Value = "ACP"
        worksheet.Cells(1, 19).Value = "ACP"
        worksheet.Cells(1, 20).Value = "CP"
        worksheet.Cells(1, 21).Value = "CAC"
        worksheet.Cells(1, 22).Value = "CAC"
        worksheet.Cells(1, 23).Value = "CAC"
        worksheet.Cells(1, 24).Value = "HRC"
        worksheet.Cells(1, 25).Value = "HRC"
        worksheet.Cells(1, 26).Value = "HRC"
        worksheet.Cells(1, 27).Value = "HRC"
        worksheet.Cells(1, 28).Value = "HRC"
        worksheet.Cells(1, 29).Value = "HRC"
        worksheet.Cells(1, 30).Value = "HRC"
        worksheet.Cells(1, 31).Value = "HRC"
        worksheet.Cells(1, 32).Value = "HRC"
        worksheet.Cells(1, 33).Value = "HRC"
        worksheet.Cells(1, 34).Value = "HRC"
        worksheet.Cells(1, 35).Value = "HRC"
        worksheet.Cells(1, 36).Value = "HRC"
        worksheet.Cells(1, 37).Value = "HRC"
        worksheet.Cells(1, 38).Value = "HRC"
        worksheet.Cells(1, 39).Value = "HRC"
        worksheet.Cells(1, 40).Value = "HRC"
        worksheet.Cells(1, 41).Value = "HRC"
        worksheet.Cells(1, 42).Value = "HRP"
        worksheet.Cells(1, 43).Value = "HRP"
        worksheet.Cells(1, 44).Value = "HRP"
        worksheet.Cells(1, 45).Value = "HRP"
        worksheet.Cells(1, 46).Value = "HRP"
        worksheet.Cells(1, 47).Value = "TC"
        worksheet.Cells(1, 48).Value = "TC"
        worksheet.Cells(1, 49).Value = "TC"

        worksheet.Cells(2, 7).Value = "7. 3A. Verify I-9"
        worksheet.Cells(2, 8).Value = "4A. 2A. Review leaves of absence"
        worksheet.Cells(2, 9).Value = "4B. 2A. Verify ee's FMLA eligibility"
        worksheet.Cells(2, 10).Value = "4B. 3A. Enter time offs for ee"
        worksheet.Cells(2, 11).Value = "4C. 3A. Enter time offs for faculty"
        worksheet.Cells(2, 12).Value = "8A. 2B. Review time off balances"
        worksheet.Cells(2, 13).Value = "5B. 3A. Review in Dean's Office"
        worksheet.Cells(2, 14).Value = "6B. Review faculty hire"
        worksheet.Cells(2, 15).Value = "8B. 2A. Review academic termination"
        worksheet.Cells(2, 16).Value = "11A. 3A. Or 3B. Review lateral"
        worksheet.Cells(2, 17).Value = "11A. 3A. Or 3B. Review lateral"
        worksheet.Cells(2, 18).Value = "11B. 3A or 3B. Review academic appointment"
        worksheet.Cells(2, 19).Value = "11B. 3A or 3B. Review academic appointment"
        worksheet.Cells(2, 20).Value = "10. 3A. Review promotion"
        worksheet.Cells(2, 21).Value = "6A. 3A. Assign costing allocations"
        worksheet.Cells(2, 22).Value = "6B. 3A. Assing costing allocations"
        worksheet.Cells(2, 23).Value = "12. 4A. Assign costing allocations"
        worksheet.Cells(2, 24).Value = "5A. 2B. Create staff position"
        worksheet.Cells(2, 25).Value = "5A. 4A. Create requistion for staff"
        worksheet.Cells(2, 26).Value = "5B. 2B. Create academic position"
        worksheet.Cells(2, 27).Value = "5B. 4A. Create requisition for faculty"
        worksheet.Cells(2, 28).Value = "6B. 1A. Enter faculty hire and appointment"
        worksheet.Cells(2, 29).Value = "8A. 1B. Initiate staff termination"
        worksheet.Cells(2, 30).Value = "8A. Run reports on terimation"
        worksheet.Cells(2, 31).Value = "8B. 1B. Initiate academic termiation"
        worksheet.Cells(2, 32).Value = "8B. 3A. Offboard and end academic appointment"
        worksheet.Cells(2, 33).Value = "8b. 4A. Run reports on termination"
        worksheet.Cells(2, 34).Value = "8C. 4A. Offboard"
        worksheet.Cells(2, 35).Value = "9A. 2A. Initiate data change"
        worksheet.Cells(2, 36).Value = "9B. 2A. Initiate Transfer"
        worksheet.Cells(2, 37).Value = "10. 2A. Initiate promotion"
        worksheet.Cells(2, 38).Value = "11A. 2A. Initiate lateral"
        worksheet.Cells(2, 39).Value = "11A. 4A. Add academic appointment"
        worksheet.Cells(2, 40).Value = "11B. 2A. Add academnic appointment"
        worksheet.Cells(2, 41).Value = "12. 2A. Initiate data change"
        worksheet.Cells(2, 42).Value = "6A. 2A. Review staff hire"
        worksheet.Cells(2, 43).Value = "8A. 2A. Review termination reason"
        worksheet.Cells(2, 44).Value = "8C. 3A. Review termination and route to UWHR"
        worksheet.Cells(2, 45).Value = "9A. 3A. Review data change"
        worksheet.Cells(2, 46).Value = "12. 3A. Review data change"
        worksheet.Cells(2, 47).Value = "1. 2C. Make work sched changes"
        worksheet.Cells(2, 48).Value = "1. 3B. Make ad hoc work sched changes"
        worksheet.Cells(2, 49).Value = "2. 2A. Run time entry reports"

        index = 7
        Do
            If worksheet.Cells(1, index).Value = "I9" Then
                worksheet.Columns(index).Interior.Color = RGB(253, 228, 207)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(247, 150, 70)
            ElseIf worksheet.Cells(1, index).Value = "ABP" Then
                worksheet.Columns(index).Interior.Color = RGB(218, 231, 246)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(83, 141, 213)
            ElseIf worksheet.Cells(1, index).Value = "ACP" Then
                worksheet.Columns(index).Interior.Color = RGB(246, 230, 230)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(218, 150, 148)
            ElseIf worksheet.Cells(1, index).Value = "CP" Then
                worksheet.Columns(index).Interior.Color = RGB(238, 234, 242)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(128, 100, 162)
            ElseIf worksheet.Cells(1, index).Value = "CAC" Then
                worksheet.Columns(index).Interior.Color = RGB(228, 223, 236)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(228, 223, 236)
            ElseIf worksheet.Cells(1, index).Value = "HRC" Then
                worksheet.Columns(index).Interior.Color = RGB(228, 228, 228)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(178, 178, 178)
            ElseIf worksheet.Cells(1, index).Value = "HRP" Then
                worksheet.Columns(index).Interior.Color = RGB(205, 233, 239)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(49, 134, 155)
            ElseIf worksheet.Cells(1, index).Value = "TC" Then
                worksheet.Columns(index).Interior.Color = RGB(241, 245, 231)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(196, 215, 155)
            End If
            index = index + 1
        Loop Until index > 50

        worksheet.Range("A1:AW3").Font.Bold = True
        worksheet.Range("A3:AW3").Borders(Excel.XlBordersIndex.xlEdgeBottom).LineStyle = Excel.XlLineStyle.xlContinuous

        Dim Dataset = worksheet.Range("A4:" & max_cell_txt)
        Dim entire_sheet = worksheet.Range("A1:" & max_cell_txt)

        worksheet.Range("A4:" & max_cell_txt).Borders(Excel.XlBordersIndex.xlInsideHorizontal).LineStyle = Excel.XlLineStyle.xlDot
        worksheet.Range("A4:" & max_cell_txt).Borders(Excel.XlBordersIndex.xlInsideHorizontal).ThemeColor = 1
        worksheet.Range("A4:" & max_cell_txt).Borders(Excel.XlBordersIndex.xlInsideHorizontal).TintAndShade = -0.14996795556505

        worksheet.Columns("G:AW").HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter

        worksheet.Range("G2:AW2").Orientation = 90
        worksheet.Range("A3:" & max_cell_txt).Autofilter
        worksheet.Columns("A:A").WrapText = True
        worksheet.Columns("F:F").WrapText = True

        worksheet.PageSetup.PrintArea = "$A$1:" & max_cell_txt
        worksheet.PageSetup.Orientation = Excel.XlPageOrientation.xlLandscape
        worksheet.PageSetup.PaperSize = Excel.XlPaperSize.xlPaper11x17
        worksheet.PageSetup.PrintTitleRows = "$1:$3"
        worksheet.PageSetup.PrintTitleColumns = "$A:$G"
        worksheet.PageSetup.CenterHeader = where_clause & Chr(10) & worksheet_name
        worksheet.PageSetup.RightHeader = "&D"

        workbook.Save()
        workbook.Close

        sSql = Nothing
        rec = Nothing
        index = Nothing
        workbook = Nothing
        worksheet = Nothing

    End Function

    Function generate_field_by_scenario_report(objExcel, conn, where_clause, file_path, worksheet_name, record_count)
        Dim sSql
        Dim rec As ADODB.Recordset
        Dim workbook
        Dim worksheet
        Dim index

        rec = New ADODB.Recordset
        sSql = "SELECT * FROM Workday_Role_Mapping_by_field_in_order_of_scenario " & where_clause
        'Debug.WriteLine(sSql)
        rec.Open(sSql, conn)
        generate_worksheet(objExcel, rec, file_path, worksheet_name)
        rec.Close()

        workbook = objExcel.Workbooks.Open(file_path)
        objExcel.Visible = False
        worksheet = workbook.Worksheets(worksheet_name)

        Dim max_column_txt = worksheet.Cells(1, 49).Address
        Dim max_cell_txt = worksheet.Cells(record_count + 3, 49).Address

        worksheet.Columns("A:A").ColumnWidth = 40
        worksheet.Columns("B:B").ColumnWidth = 8
        worksheet.Columns("C:C").ColumnWidth = 15
        worksheet.Columns("D:D").ColumnWidth = 15
        worksheet.Columns("E:E").ColumnWidth = 10
        worksheet.Columns("F:F").ColumnWidth = 50

        worksheet.Columns("G:AW").ColumnWidth = 3

        worksheet.Rows("1").Insert
        worksheet.Rows("1").Insert
        'worksheet.Range("G2:N2").Cut
        worksheet.Cells(1, 7).Value = "TC"
        worksheet.Cells(1, 8).Value = "TC"
        worksheet.Cells(1, 9).Value = "TC"
        worksheet.Cells(1, 10).Value = "ABP"
        worksheet.Cells(1, 11).Value = "ABP"
        worksheet.Cells(1, 12).Value = "ABP"
        worksheet.Cells(1, 13).Value = "ABP"
        worksheet.Cells(1, 14).Value = "HRC"
        worksheet.Cells(1, 15).Value = "HRC"
        worksheet.Cells(1, 16).Value = "HRC"
        worksheet.Cells(1, 17).Value = "ACP"
        worksheet.Cells(1, 18).Value = "HRC"
        worksheet.Cells(1, 19).Value = "HRP"
        worksheet.Cells(1, 20).Value = "CAC"
        worksheet.Cells(1, 21).Value = "HRC"
        worksheet.Cells(1, 22).Value = "CAC"
        worksheet.Cells(1, 23).Value = "ACP"
        worksheet.Cells(1, 24).Value = "I9"
        worksheet.Cells(1, 25).Value = "HRC"
        worksheet.Cells(1, 26).Value = "HRP"
        worksheet.Cells(1, 27).Value = "ABP"
        worksheet.Cells(1, 28).Value = "HRC"
        worksheet.Cells(1, 29).Value = "HRC"
        worksheet.Cells(1, 30).Value = "ACP"
        worksheet.Cells(1, 31).Value = "HRC"
        worksheet.Cells(1, 32).Value = "HRC"
        worksheet.Cells(1, 33).Value = "HRP"
        worksheet.Cells(1, 34).Value = "HRC"
        worksheet.Cells(1, 35).Value = "HRC"
        worksheet.Cells(1, 36).Value = "HRP"
        worksheet.Cells(1, 37).Value = "HRC"
        worksheet.Cells(1, 38).Value = "HRC"
        worksheet.Cells(1, 39).Value = "CP"
        worksheet.Cells(1, 40).Value = "HRC"
        worksheet.Cells(1, 41).Value = "ACP"
        worksheet.Cells(1, 42).Value = "ACP"
        worksheet.Cells(1, 43).Value = "HRC"
        worksheet.Cells(1, 44).Value = "HRC"
        worksheet.Cells(1, 45).Value = "ACP"
        worksheet.Cells(1, 46).Value = "ACP"
        worksheet.Cells(1, 47).Value = "HRC"
        worksheet.Cells(1, 48).Value = "HRP"
        worksheet.Cells(1, 49).Value = "CAC"


        worksheet.Cells(2, 7).Value = "1. 2C. Make work sched changes"
        worksheet.Cells(2, 8).Value = "1. 3B. Make ad hoc work sched changes"
        worksheet.Cells(2, 9).Value = "2. 2A. Run time entry reports"
        worksheet.Cells(2, 10).Value = "4A. 2A. Review leaves of absence"
        worksheet.Cells(2, 11).Value = "4B. 2A. Verify ee's FMLA eligibility"
        worksheet.Cells(2, 12).Value = "4B. 3A. Enter time offs for ee"
        worksheet.Cells(2, 13).Value = "4C. 3A. Enter time offs for faculty"
        worksheet.Cells(2, 14).Value = "5A. 2B. Create staff position"
        worksheet.Cells(2, 15).Value = "5A. 4A. Create requistion for staff"
        worksheet.Cells(2, 16).Value = "5B. 2B. Create academic position"
        worksheet.Cells(2, 17).Value = "5B. 3A. Review in Dean's Office"
        worksheet.Cells(2, 18).Value = "5B. 4A. Create requisition for faculty"
        worksheet.Cells(2, 19).Value = "6A. 2A. Review staff hire"
        worksheet.Cells(2, 20).Value = "6A. 3A. Assign costing allocations"
        worksheet.Cells(2, 21).Value = "6B. 1A. Enter faculty hire and appointment"
        worksheet.Cells(2, 22).Value = "6B. 3A. Assing costing allocations"
        worksheet.Cells(2, 23).Value = "6B. Review faculty hire"
        worksheet.Cells(2, 24).Value = "7. 3A. Verify I-9"
        worksheet.Cells(2, 25).Value = "8A. 1B. Initiate staff termination"
        worksheet.Cells(2, 26).Value = "8A. 2A. Review termination reason"
        worksheet.Cells(2, 27).Value = "8A. 2B. Review time off balances"
        worksheet.Cells(2, 28).Value = "8A. Run reports on terimation"
        worksheet.Cells(2, 29).Value = "8B. 1B. Initiate academic termiation"
        worksheet.Cells(2, 30).Value = "8B. 2A. Review academic termination"
        worksheet.Cells(2, 31).Value = "8B. 3A. Offboard and end academic appointment"
        worksheet.Cells(2, 32).Value = "8b. 4A. Run reports on termination"
        worksheet.Cells(2, 33).Value = "8C. 3A. Review termination and route to UWHR"
        worksheet.Cells(2, 34).Value = "8C. 4A. Offboard"
        worksheet.Cells(2, 35).Value = "9A. 2A. Initiate data change"
        worksheet.Cells(2, 36).Value = "9A. 3A. Review data change"
        worksheet.Cells(2, 37).Value = "9B. 2A. Initiate Transfer"
        worksheet.Cells(2, 38).Value = "10. 2A. Initiate promotion"
        worksheet.Cells(2, 39).Value = "10. 3A. Review promotion"
        worksheet.Cells(2, 40).Value = "11A. 2A. Initiate lateral"
        worksheet.Cells(2, 41).Value = "11A. 3A. Or 3B. Review lateral"
        worksheet.Cells(2, 42).Value = "11A. 3A. Or 3B. Review lateral"
        worksheet.Cells(2, 43).Value = "11A. 4A. Add academic appointment"
        worksheet.Cells(2, 44).Value = "11B. 2A. Add academnic appointment"
        worksheet.Cells(2, 45).Value = "11B. 3A or 3B. Review academic appointment"
        worksheet.Cells(2, 46).Value = "11B. 3A or 3B. Review academic appointment"
        worksheet.Cells(2, 47).Value = "12. 2A. Initiate data change"
        worksheet.Cells(2, 48).Value = "12. 3A. Review data change"
        worksheet.Cells(2, 49).Value = "12. 4A. Assign costing allocations"

        index = 7
        Do
            If worksheet.Cells(1, index).Value = "I9" Then
                worksheet.Columns(index).Interior.Color = RGB(253, 228, 207)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(247, 150, 70)
            ElseIf worksheet.Cells(1, index).Value = "ABP" Then
                worksheet.Columns(index).Interior.Color = RGB(218, 231, 246)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(83, 141, 213)
            ElseIf worksheet.Cells(1, index).Value = "ACP" Then
                worksheet.Columns(index).Interior.Color = RGB(246, 230, 230)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(218, 150, 148)
            ElseIf worksheet.Cells(1, index).Value = "CP" Then
                worksheet.Columns(index).Interior.Color = RGB(238, 234, 242)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(128, 100, 162)
            ElseIf worksheet.Cells(1, index).Value = "CAC" Then
                worksheet.Columns(index).Interior.Color = RGB(228, 223, 236)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(228, 223, 236)
            ElseIf worksheet.Cells(1, index).Value = "HRC" Then
                worksheet.Columns(index).Interior.Color = RGB(228, 228, 228)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(178, 178, 178)
            ElseIf worksheet.Cells(1, index).Value = "HRP" Then
                worksheet.Columns(index).Interior.Color = RGB(205, 233, 239)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(49, 134, 155)
            ElseIf worksheet.Cells(1, index).Value = "TC" Then
                worksheet.Columns(index).Interior.Color = RGB(241, 245, 231)
                worksheet.Range(worksheet.Cells(1, index), worksheet.Cells(3, index)).Interior.Color = RGB(196, 215, 155)
            End If
            index = index + 1
        Loop Until index > 50

        Dim Dataset = worksheet.Range("A4:" & max_cell_txt)
        Dim entire_sheet = worksheet.Range("A1:" & max_cell_txt)

        worksheet.Range("A1:AW3").Font.Bold = True

        worksheet.Range("A3:AW3").Borders(Excel.XlBordersIndex.xlEdgeBottom).LineStyle = Excel.XlLineStyle.xlContinuous

        worksheet.Range("A4:" & max_cell_txt).Borders(Excel.XlBordersIndex.xlInsideHorizontal).LineStyle = Excel.XlLineStyle.xlDot
        worksheet.Range("A4:" & max_cell_txt).Borders(Excel.XlBordersIndex.xlInsideHorizontal).ThemeColor = 1
        worksheet.Range("A4:" & max_cell_txt).Borders(Excel.XlBordersIndex.xlInsideHorizontal).TintAndShade = -0.14996795556505

        worksheet.Columns("G:AW").HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter

        worksheet.Range("G2:AW2").Orientation = 90
        worksheet.Range("A3:" & max_cell_txt).Autofilter
        worksheet.Columns("A:A").WrapText = True
        worksheet.Columns("F:F").WrapText = True

        worksheet.PageSetup.PrintArea = "$A$1:" & max_cell_txt
        worksheet.PageSetup.Orientation = Excel.XlPageOrientation.xlLandscape
        worksheet.PageSetup.PaperSize = Excel.XlPaperSize.xlPaper11x17
        worksheet.PageSetup.PrintTitleRows = "$1:$3"
        worksheet.PageSetup.PrintTitleColumns = "$A:$G"
        worksheet.PageSetup.CenterHeader = where_clause & Chr(10) & worksheet_name
        worksheet.PageSetup.RightHeader = "&D"

        workbook.Save()
        workbook.Close

        sSql = Nothing
        rec = Nothing
        index = Nothing
        workbook = Nothing
        worksheet = Nothing

    End Function


    Function generate_role_confirmation_tool(objExcel, conn, where_clause, file_path, worksheet_name, record_count)
        Dim sSql
        Dim rec As ADODB.Recordset
        Dim workbook
        Dim worksheet

        rec = New ADODB.Recordset
        sSql = "SELECT * FROM Workday_Role_Mapping_By_Role_Transpose " & where_clause
        rec.Open(sSql, conn)

        Dim field_count = rec.Fields.Count
        generate_transposed_worksheet(objExcel, rec, file_path, worksheet_name, record_count)
        rec.Close()

        workbook = objExcel.Workbooks.Open(file_path)
        worksheet = workbook.Worksheets(worksheet_name)

        worksheet.Columns(1).Insert
        worksheet.Columns(1).Insert
        worksheet.Columns(1).Insert

        worksheet.Columns("A:A").ColumnWidth = 6        'Workday Code
        worksheet.Columns("B:B").ColumnWidth = 30       'Workday Role
        worksheet.Columns("C:C").ColumnWidth = 75       'Workday Role Description

        'worksheet.Range("G2:N2").Cut

        worksheet.Cells(1, 1).Value = "Code"
        worksheet.Cells(2, 1).Value = "I9"
        worksheet.Cells(3, 1).Value = "ABP"
        worksheet.Cells(4, 1).Value = "AD"
        worksheet.Cells(5, 1).Value = "AE"
        worksheet.Cells(6, 1).Value = "ACP"
        worksheet.Cells(7, 1).Value = "CP"
        worksheet.Cells(8, 1).Value = "CM"
        worksheet.Cells(9, 1).Value = "CAC"
        worksheet.Cells(10, 1).Value = "HRC"
        worksheet.Cells(11, 1).Value = "HRE"
        worksheet.Cells(12, 1).Value = "HRP"
        worksheet.Cells(13, 1).Value = "RP"
        worksheet.Cells(14, 1).Value = "TC"

        worksheet.Cells(1, 2).Value = "Workday Role"
        worksheet.Cells(2, 2).Value = "I-9 Coordinator"
        worksheet.Cells(3, 2).Value = "Absence Partner"
        worksheet.Cells(4, 2).Value = "Academic Dean"
        worksheet.Cells(5, 2).Value = "Academic Executive"
        worksheet.Cells(6, 2).Value = "Academic Partner"
        worksheet.Cells(7, 2).Value = "Compensation Partner"
        worksheet.Cells(8, 2).Value = "Cost Center Manager"
        worksheet.Cells(9, 2).Value = "Costing Allocations Coordinator"
        worksheet.Cells(10, 2).Value = "HR Coordinator"
        worksheet.Cells(11, 2).Value = "HR Executive"
        worksheet.Cells(12, 2).Value = "HR Partner"
        worksheet.Cells(13, 2).Value = "Recruiting Partner"
        worksheet.Cells(14, 2).Value = "Time Coordinator"

        worksheet.Cells(1, 3).Value = "Role Description"
        worksheet.Cells(2, 3).Value = "Collects I-9 information for their assigned organization and verifies that I-9 information is valid."
        worksheet.Cells(3, 3).Value = "This role performs absence management tasks for assigned organizations.  May enter time off for a worker. May request and return employee from a leave of absence."
        worksheet.Cells(4, 3).Value = "This role is responsible for determining organizational objectives at the school, college, and/or campus level.  Approves HR transactions related to academic personnel across multiple supervisory organizations."
        worksheet.Cells(5, 3).Value = "This role can view all HR setup and operational data for assigned organizations.  This role is the highest departmental role and has approval authority for Workday business processes related to academic personnel."
        worksheet.Cells(6, 3).Value = "This role has administrative responsibilities related to a group of academic personnel designated by supervisory organization.  Has review and/or approval authority for Workday business processes related to academic personnel."
        worksheet.Cells(7, 3).Value = "This role has compensation related duties For a group of employees for assigned supervisory organizations."
        worksheet.Cells(8, 3).Value = "This role manages the budget related to their cost center. Has approval authority on business processes where an employee will be added to the supervisory organization tied to their cost center."
        worksheet.Cells(9, 3).Value = "This role enters costing allocation information for employees and is responsible for payroll transactions within a department; they also have the ability to change budgets."
        worksheet.Cells(10, 3).Value = "This individual has administrative responsibilities for a group of employees designated by a supervisory organization.  Can initiate most HR-related actions."
        worksheet.Cells(11, 3).Value = "This role can view all HR setup and operational data for assigned organizations.  This role is the highest departmental role and has approval authority for Workday business processes related to personnel"
        worksheet.Cells(12, 3).Value = "This role has administrative responsibilities related to a group of employees designated by supervisory organization.  Has review and/or approval authority for Workday business processes."
        worksheet.Cells(13, 3).Value = "This individual is a skilled professional responsible for sourcing, screening, recruiting, and executing recruitment activities for assigned supervisory organizations."
        worksheet.Cells(14, 3).Value = "This role performs timesheet management functions for assigned organizations.  May correct time and enter time for worker.   Runs reports to ensure all timesheets are entered and approved."


        Dim max_column_txt = worksheet.Cells(1, record_count + 3).Address
        Dim max_cell_txt = worksheet.Cells(14, record_count + 3).Address

        worksheet.Range("A1:" & max_column_txt).Font.Bold = True

        'worksheet.Range("A2:N2").Borders(Excel.XlBordersIndex.xlEdgeBottom).LineStyle = Excel.XlLineStyle.xlContinuous

        'worksheet.Range("A3:N2000").Borders(Excel.XlBordersIndex.xlInsideHorizontal).LineStyle = Excel.XlLineStyle.xlDot
        'worksheet.Range("A3:N2000").Borders(Excel.XlBordersIndex.xlInsideHorizontal).ThemeColor = 1
        'worksheet.Range("A3:N2000").Borders(Excel.XlBordersIndex.xlInsideHorizontal).TintAndShade = -0.14996795556505

        'worksheet.Columns("D:N").HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter

        worksheet.Range("D1:" & max_column_txt).Orientation = 90
        worksheet.Range("D1:" & max_column_txt).RowHeight = 100
        worksheet.Range("D1:" & max_column_txt).ColumnWidth = 9
        worksheet.Range("D1:" & max_cell_txt).VerticalAlignment = Excel.XlVAlign.xlVAlignCenter
        worksheet.Range("D1:" & max_cell_txt).HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter
        worksheet.Range("D1:" & max_column_txt).WrapText = True

        Dim selection = worksheet.Range("A1:" & max_cell_txt)

        With selection.Borders(Excel.XlBordersIndex.xlEdgeLeft)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With
        With selection.Borders(Excel.XlBordersIndex.xlEdgeTop)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With
        With selection.Borders(Excel.XlBordersIndex.xlEdgeBottom)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With
        With selection.Borders(Excel.XlBordersIndex.xlEdgeRight)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With
        With selection.Borders(Excel.XlBordersIndex.xlInsideVertical)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With
        With selection.Borders(Excel.XlBordersIndex.xlInsideHorizontal)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With

        selection = worksheet.Range("A2:" & max_cell_txt)

        With selection.Borders(Excel.XlBordersIndex.xlEdgeLeft)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With
        With selection.Borders(Excel.XlBordersIndex.xlEdgeTop)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With
        With selection.Borders(Excel.XlBordersIndex.xlEdgeBottom)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With
        With selection.Borders(Excel.XlBordersIndex.xlEdgeRight)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With
        With selection.Borders(Excel.XlBordersIndex.xlInsideVertical)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With
        With selection.Borders(Excel.XlBordersIndex.xlInsideHorizontal)
            .LineStyle = Excel.XlLineStyle.xlContinuous
            .ColorIndex = 0
            .TintAndShade = 0
            .Weight = Excel.XlBorderWeight.xlThin
        End With

        worksheet.Columns("B:C").WrapText = True

        worksheet.Range("A1:" & max_cell_txt).Autofilter

        worksheet.Range("A1:" & max_column_txt).Interior.Color = RGB(216, 191, 235)

        worksheet.PageSetup.PrintArea = "$A$1:" & max_cell_txt
        worksheet.PageSetup.Orientation = Excel.XlPageOrientation.xlLandscape
        worksheet.PageSetup.PaperSize = Excel.XlPaperSize.xlPaper11x17
        worksheet.PageSetup.PrintTitleRows = "$1:$1"
        worksheet.PageSetup.PrintTitleColumns = "$A:$C"
        worksheet.PageSetup.CenterHeader = where_clause & Chr(10) & worksheet_name
        worksheet.PageSetup.RightHeader = "&D"

        workbook.Save()
        workbook.Close

        sSql = Nothing
        rec = Nothing
        workbook = Nothing
        worksheet = Nothing

    End Function

    Function generate_worksheet(objExcel, recordset, file_path, worksheet_name)
        Dim Workbook
        Dim Worksheet
        Dim fieldCount
        Dim recArray
        Dim recCount

        objExcel.Visible = False
        objExcel.DisplayAlerts = 0 ' Don't display any messages about conversion and so forth
        Workbook = objExcel.Workbooks.Open(file_path)
        Worksheet = Workbook.Worksheets(worksheet_name)

        ' Copy field names to the first row of the worksheet
        fieldCount = recordset.Fields.Count
        For iCol = 1 To fieldCount
            Worksheet.Cells(1, iCol).Value = recordset.Fields(iCol - 1).Name
        Next

        ' Check version of Excel
        If Val(Mid(objExcel.Version, 1, InStr(1, objExcel.Version, ".") - 1)) > 8 Then
            'EXCEL 2000,2002,2003, or 2007: Use CopyFromRecordset

            ' Copy the recordset to the worksheet, starting in cell A2
            Worksheet.Cells(2, 1).CopyFromRecordset(recordset)
            'Note: CopyFromRecordset will fail if the recordset
            'contains an OLE object field or array data such
            'as hierarchical recordsets

        Else
            'EXCEL 97 or earlier: Use GetRows then copy array to Excel

            ' Copy recordset to an array
            recArray = recordset.GetRows
            'Note: GetRows returns a 0-based array where the first
            'dimension contains fields and the second dimension
            'contains records. We will transpose this array so that
            'the first dimension contains records, allowing the
            'data to appears properly when copied to Excel

            ' Determine number of records

            recCount = UBound(recArray, 2) + 1 '+ 1 since 0-based array


            ' Check the array for contents that are not valid when
            ' copying the array to an Excel worksheet
            For iCol = 0 To fieldCount - 1
                For iRow = 0 To recCount - 1
                    ' Take care of Date fields
                    If IsDate(recArray(iCol, iRow)) Then
                        recArray(iCol, iRow) = Format(recArray(iCol, iRow))
                        ' Take care of OLE object fields or array fields
                    ElseIf IsArray(recArray(iCol, iRow)) Then
                        recArray(iCol, iRow) = "Array Field"
                    End If
                Next iRow 'next record
            Next iCol 'next field

            ' Transpose and Copy the array to the worksheet,
            ' starting in cell A2
            Worksheet.Cells(2, 1).Resize(recCount, fieldCount).Value =
                TransposeDim(recArray)
        End If

        ' Auto-fit the column widths and row heights
        objExcel.Selection.CurrentRegion.Columns.AutoFit
        objExcel.Selection.CurrentRegion.Rows.AutoFit

        Workbook.SaveAs(FileName:=file_path)
        Workbook.Close()

        Workbook = Nothing
        Worksheet = Nothing

    End Function

    Function generate_transposed_worksheet(objExcel, recordset, file_path, worksheet_name, record_count)
        Dim Workbook
        Dim Worksheet
        Dim fieldCount
        Dim recArray
        Dim recCount

        objExcel.Visible = False
        objExcel.DisplayAlerts = 0 ' Don't display any messages about conversion and so forth
        Workbook = objExcel.Workbooks.Open(file_path)
        Worksheet = Workbook.Worksheets(worksheet_name)
        Worksheet.Select

        ' Copy field names to the first row of the worksheet
        fieldCount = recordset.Fields.Count

        For iCol = 1 To fieldCount
            Worksheet.Cells(1, iCol).Value = recordset.Fields(iCol - 1).Name
        Next

        ' Check version of Excel
        If Val(Mid(objExcel.Version, 1, InStr(1, objExcel.Version, ".") - 1)) > 8 Then
            'EXCEL 2000,2002,2003, or 2007: Use CopyFromRecordset

            ' Copy the recordset to the worksheet, starting in cell A2
            Worksheet.Range("A1").CopyFromRecordset(recordset)
            Dim max_cell_txt = Worksheet.cells(record_count, fieldCount).Address
            Dim min_cell_txt = Worksheet.cells(1, 1).Address
            Dim range = min_cell_txt & ":" & max_cell_txt
            Dim t_range = Worksheet.cells(record_count + 1, 1).Address

            Worksheet.Range(range).Copy
            Debug.WriteLine(t_range)
            Worksheet.Range(t_range).Select
            Worksheet.Range(t_range).PasteSpecial(Paste:=Excel.XlPasteType.xlPasteValues,
                    Transpose:=True)
            'Note: CopyFromRecordset will fail if the recordset
            'contains an OLE object field or array data such
            'as hierarchical recordsets

        Else
            'EXCEL 97 or earlier: Use GetRows then copy array to Excel

            ' Copy recordset to an array
            recArray = recordset.GetRows
            'Note: GetRows returns a 0-based array where the first
            'dimension contains fields and the second dimension
            'contains records. We will transpose this array so that
            'the first dimension contains records, allowing the
            'data to appears properly when copied to Excel

            ' Determine number of records

            recCount = UBound(recArray, 2) + 1 '+ 1 since 0-based array

            ' Check the array for contents that are not valid when
            ' copying the array to an Excel worksheet
            For iCol = 0 To fieldCount - 1
                For iRow = 0 To recCount - 1
                    ' Take care of Date fields
                    If IsDate(recArray(iCol, iRow)) Then
                        recArray(iCol, iRow) = Format(recArray(iCol, iRow))
                        ' Take care of OLE object fields or array fields
                    ElseIf IsArray(recArray(iCol, iRow)) Then
                        recArray(iCol, iRow) = "Array Field"
                    End If
                Next iRow 'next record
            Next iCol 'next field

            ' Transpose and Copy the array to the worksheet,
            ' starting in cell A2
            Worksheet.Cells(2, 1).Resize(recCount, fieldCount).Value =
                TransposeDim(recArray)
        End If

        Worksheet.Rows("1:" & record_count.ToString).delete

        Worksheet.Rows("2:2").delete

        ' Auto-fit the column widths and row heights
        objExcel.Selection.CurrentRegion.Columns.AutoFit
        objExcel.Selection.CurrentRegion.Rows.AutoFit

        Workbook.SaveAs(FileName:=file_path)
        Workbook.Close()

        Workbook = Nothing
        Worksheet = Nothing

    End Function

    Function generate_generic_report(objExcel, conn, sSql, file_path, worksheet_name)
        Dim rec As ADODB.Recordset
        Dim index As Integer
        Dim workbook
        Dim worksheet
        Dim MaxCol = 0
        Dim MaxRow = 3000

        rec = New ADODB.Recordset
        rec.Open(sSql, conn)
        generate_worksheet(objExcel, rec, file_path, worksheet_name)

        Debug.WriteLine("FindRecordCount :" & rec.Fields.Count)
        rec.Close()

        MaxCol = rec.Fields.Count
        Dim MaxColTxt = MaxCol.ToString
        Dim MaxRowTxt = MaxRow.ToString

        workbook = objExcel.Workbooks.Open(file_path)
        objExcel.Visible = True

        worksheet = workbook.Worksheets(worksheet_name)

        Dim MaxCell = worksheet.Cells(MaxRow, MaxCol)
        Dim lastColumnCell = worksheet.Cells(1, MaxCol)
        Dim StartCell = worksheet.Cells(1, 1)
        Dim lastRowCell = worksheet.Cells(MaxRow, 1)
        Dim firstDataCell = worksheet.Cells(2, 1)
        Dim Full_set = worksheet.Range(StartCell, MaxCell)
        Dim Dataset = worksheet.Range(firstDataCell, MaxCell)

        worksheet.Range("$1:$1").Font.Bold = True

        worksheet.Range(StartCell, lastColumnCell).Borders(Excel.XlBordersIndex.xlEdgeBottom).LineStyle = Excel.XlLineStyle.xlContinuous

        Dataset.Borders(Excel.XlBordersIndex.xlInsideHorizontal).LineStyle = Excel.XlLineStyle.xlDot
        Dataset.Borders(Excel.XlBordersIndex.xlInsideHorizontal).ThemeColor = 1
        Dataset.Borders(Excel.XlBordersIndex.xlInsideHorizontal).TintAndShade = -0.14996795556505

        'worksheet.Columns("G:AW").HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter

        Full_set.Autofilter

        'worksheet.PageSetup.PrintArea = Full_set
        worksheet.PageSetup.Orientation = Excel.XlPageOrientation.xlLandscape
        worksheet.PageSetup.PaperSize = Excel.XlPaperSize.xlPaper11x17
        worksheet.PageSetup.PrintTitleRows = "$1:$1"
        'worksheet.PageSetup.PrintTitleColumns = "$A:$G"
        worksheet.PageSetup.CenterHeader = worksheet_name
        worksheet.PageSetup.RightHeader = "&D"

        workbook.Save()
        workbook.Close

        sSql = Nothing
        rec = Nothing
        index = Nothing
        workbook = Nothing
        worksheet = Nothing

    End Function


End Module
