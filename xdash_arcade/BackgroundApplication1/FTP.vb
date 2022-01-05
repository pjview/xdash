Module FTP
    Private FTP_Client As New System.Net.Sockets.TcpClient
    Private FTP_Data As System.Net.Sockets.TcpClient
    'Dim FTP_EndPoint As New System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.168.148.109"), 21)
    Private CMD_EndPoint As System.Net.IPEndPoint
    Private DATA_EndPoint As System.Net.IPEndPoint


    Private FTP_Stream As System.Net.Sockets.NetworkStream
    Private DATA_Stream As System.Net.Sockets.NetworkStream

    Private TXBuffer(256) As Byte
    Private RXBuffer(256) As Byte
    Private DummyBuffer(256) As Byte
    Private Ascii As System.Text.Encoding = System.Text.Encoding.ASCII
    Private ErrorStr As String = ""
    Private ConnectionOK As Boolean = False
    Private TransferMode As Boolean = False
    Private TransferBusy As Boolean = False
    Private CommandIdle As Boolean = False
    Private NOOP() As Byte = System.Text.Encoding.ASCII.GetBytes("NOOP" & vbCrLf)

    Public Async Function FTPConnectAsync(IPAddress As System.Net.IPAddress, UserStr As String, PassStr As String) As Task(Of Boolean)
        Dim Finish As Boolean = False
        Dim ProcessTimeout As Integer = 20
        Dim ProcessTimeCount As Integer = 0
        Dim NeedRespond As Boolean = False
        Dim RData As String = ""
        Dim RValue As Integer = 0
        Dim AIDX As Integer = 0
        Dim BIDX As Integer = 0
        Dim CIDX As Integer = 0
        Dim DIDX As Integer = 0
        Dim Command_Accept As Boolean = False
        Dim Login_OK As Boolean = False
        Dim PData(10) As Integer

        CommandIdle = False
        If Not FTP_Client.Connected Then
            Try
                FTP_Client.ReceiveTimeout = 5000
                FTP_Client.SendTimeout = 5000
                Await FTP_Client.ConnectAsync(IPAddress, 21)
            Catch
                Debug.WriteLine("FTP Connection ERROR")
                Return False
            End Try
        End If
        FTP_Stream = FTP_Client.GetStream()
        NeedRespond = True

        Do Until Finish = True
            If NeedRespond Then
                RData = ReadNet()
                NeedRespond = False
            End If
            Select Case GetFTPRespond(RData)
                Case = 200 ' Command Accept
                    Command_Accept = True
                    If DIDX >= 0 And DIDX < 3 Then DIDX += 1
                Case = 220 ' Server Ready
                    WriteNet("USER " & UserStr & vbCrLf)
                    NeedRespond = True
                Case = 230 ' Login OK
                    Login_OK = True
                    DIDX = 0
                Case = 227 ' Passive Mode OK
                    AIDX = RData.LastIndexOf("(")
                    BIDX = RData.LastIndexOf(")")
                    If AIDX > 0 And BIDX > 0 Then
                        If BIDX > AIDX Then
                            CIDX = 0
                            For Each FData As String In RData.Substring(AIDX + 1, BIDX - (AIDX + 1)).Split(",")
                                If CIDX < 6 Then PData(CIDX) = GetFTPRespond(FData)
                                CIDX += 1
                            Next
                        End If
                    End If
                    If CIDX = 6 Then
                        DATA_EndPoint = New System.Net.IPEndPoint(System.Net.IPAddress.Parse((PData(0).ToString & "." & PData(1).ToString & "." & PData(2).ToString & "." & PData(3).ToString)), (PData(4) * 256) + PData(5))
                        Finish = True
                        ConnectionOK = True
                    Else
                        ConnectionOK = False
                    End If
                    Finish = True
                Case = 331 ' Password require
                    WriteNet("PASS " & PassStr & vbCrLf)
                    NeedRespond = True
                Case = 530 ' Permission denine
                    ErrorStr = "Permission denine"
                    Finish = True

            End Select

            If Login_OK Then
                Select Case DIDX
                    Case = 0
                        WriteNet("TYPE A" & vbCrLf) 'type "A" = ASCII , "I" = Binary
                        NeedRespond = True
                    Case = 1
                        WriteNet("STRU F" & vbCrLf)
                        NeedRespond = True
                    Case = 2
                        WriteNet("MODE S" & vbCrLf)
                        NeedRespond = True
                    Case = 3
                        WriteNet("PASV" & vbCrLf)
                        NeedRespond = True
                        DIDX = -1
                End Select
            End If

            If NeedRespond Then ProcessTimeCount = 0
            ProcessTimeCount += 1
            If ProcessTimeCount > ProcessTimeout Then
                ErrorStr = "Operation Timeout"
                Exit Do
            End If
            Task.Delay(100).Wait()
        Loop

        Return True
    End Function

    Public Function Connected() As Boolean
        Return ConnectionOK
    End Function

    Public Function GetErrStr() As String
        Return ErrorStr
    End Function

    Public Async Function FileCreate(FileName As String, ReqPASV As Boolean, WaitConfirmation As Boolean) As Task(Of Boolean)
        Dim Finish As Boolean = False
        Dim RData As String = ""
        Dim AIDX As Integer = 0
        Dim BIDX As Integer = 0
        Dim CIDX As Integer = 0
        Dim PData(10) As Integer
        Dim ResCODE As Integer = 0
        'If FTP_Data Is Nothing Then FTP_Data = New System.Net.Sockets.TcpClient
        FTP_Data = New System.Net.Sockets.TcpClient
        'If DATA_Stream Is Nothing Then DATA_Stream = New System.Net.Sockets.NetworkStream
        CommandIdle = False
        Debug.WriteLine("FTP connect OK")
        If Not FTP_Data.Connected Then
            'WriteNet("APPE " & FileName & vbCrLf)
            CommandIdle = False
            If ReqPASV Then
                Debug.WriteLine("FTP create data channel")
                WriteNet("PASV" & vbCrLf)
                For i As Integer = 0 To 10
                    RData = ReadNet()
                    ResCODE = GetFTPRespond(RData)
                    If ResCODE = 227 Then
                        Finish = True
                        Exit For
                    End If
                    Task.Delay(50).Wait()
                Next
                If Finish Then
                    AIDX = RData.LastIndexOf("(")
                    BIDX = RData.LastIndexOf(")")
                    If AIDX > 0 And BIDX > 0 Then
                        If BIDX > AIDX Then
                            CIDX = 0
                            For Each FData As String In RData.Substring(AIDX + 1, BIDX - (AIDX + 1)).Split(",")
                                If CIDX < 6 Then PData(CIDX) = GetFTPRespond(FData)
                                CIDX += 1
                            Next
                        End If
                    End If
                    If CIDX = 6 Then
                        DATA_EndPoint = New System.Net.IPEndPoint(System.Net.IPAddress.Parse((PData(0).ToString & "." & PData(1).ToString & "." & PData(2).ToString & "." & PData(3).ToString)), (PData(4) * 256) + PData(5))
                        ConnectionOK = True
                    Else
                        ConnectionOK = False
                    End If
                    Debug.WriteLine("FTP channel created")
                Else
                    Debug.WriteLine("FTP channel error")
                End If
                Finish = False
            End If
            Debug.WriteLine("FTP create new file")
            WriteNet("STOR " & FileName & vbCrLf)
            If WaitConfirmation Then
                RData = ReadNet()
                If RData.StartsWith("150") Then
                    Debug.WriteLine("FTP F BEGIN")
                    Await FTP_Data.ConnectAsync(DATA_EndPoint.Address, DATA_EndPoint.Port)
                    Debug.WriteLine("FTP F OK")
                    DATA_Stream = FTP_Data.GetStream()
                    Finish = True
                End If
            Else
                Debug.WriteLine("FTP F BEGIN")
                Await FTP_Data.ConnectAsync(DATA_EndPoint.Address, DATA_EndPoint.Port)
                Debug.WriteLine("FTP F OK")
                DATA_Stream = FTP_Data.GetStream()
                Finish = True
            End If
            Do
                RData = ReadNet()
                If GetFTPRespond(RData) = 150 Then Exit Do
                If RData.Length < 1 Then Exit Do
            Loop
            CommandIdle = False
        End If
        TransferMode = Finish
        TransferBusy = False
        Return Finish
    End Function

    Public Async Sub KeepAlive()
        If FTP_Client.Connected Then
            If CommandIdle Then
                If FTP_Stream IsNot Nothing Then
                    If FTP_Stream.CanWrite Then
                        Try
                            Await FTP_Stream.WriteAsync(NOOP, 0, NOOP.Length)
                            Await FTP_Stream.ReadAsync(DummyBuffer, 0, DummyBuffer.Length)
                        Catch ex As Exception
                            Debug.WriteLine("FTP TX ERROR : " & ex.Message)
                            CommandIdle = False
                        End Try
                    Else
                        Debug.WriteLine("FTP (NOT-READY)")
                        CommandIdle = False
                    End If
                End If
            End If
        End If
    End Sub

    Public Async Function FileSend(Data As String) As Task(Of Boolean)
        Dim Res As Boolean = False
        If TransferBusy = True Or TransferMode = False Then Return False
        If FTP_Data IsNot Nothing Then
            Try
                If DATA_Stream.CanWrite Then
                    TransferBusy = True
                    TXBuffer = Ascii.GetBytes(Data)
                    Await DATA_Stream.WriteAsync(TXBuffer, 0, TXBuffer.Length)
                    TransferBusy = False
                    Res = True
                End If
            Catch ex As Exception
                Debug.WriteLine("FTP ERROR (A): " & ex.Message)
                Debug.WriteLine("FTP ERROR (B): " & ex.Source)
                DATA_Stream.Dispose()
                FTP_Data.Dispose()
                TransferMode = False
            End Try
        End If
        Return Res
    End Function

    Public Function FileClose() As Boolean
        Dim Res As Boolean = False
        Dim RData As String = ""
        TransferMode = False
        If FTP_Data IsNot Nothing Then
            If FTP_Data.Connected Then
                DATA_Stream.Dispose()
                FTP_Data.Dispose()
                Res = True
                Do
                    RData = ReadNet()
                    If GetFTPRespond(RData) = 226 Then Exit Do
                    If RData.Length < 1 Then Exit Do
                Loop
                CommandIdle = True
            End If
        End If
        Return Res
    End Function

    Public Function FileInProgress() As Boolean
        Return TransferMode
    End Function

    Private Function GetFTPRespond(Data As String) As Integer
        Dim Res As Integer = 0
        Dim NLenght As Integer = 1
        Dim NFound As Boolean = False
        Dim CharArray() As Byte
        If Data <> "" Then
            CharArray = Ascii.GetBytes(Data)
            For i As Integer = 0 To CharArray.Length - 1
                If CharArray(i) >= 48 And CharArray(i) <= 57 Then
                    Res = (Res * NLenght) + (CharArray(i) - 48)
                    NLenght = 10
                    NFound = True
                Else
                    Exit For
                End If
            Next
        End If
        If Not NFound Then Res = -1
        Return Res
    End Function

    Private Function ReadNet() As String
        Dim RXLenght As Integer = 0
        Dim Res As String = ""
        If FTP_Stream IsNot Nothing Then
            If FTP_Stream.CanRead Then
                Try
                    RXLenght = FTP_Stream.Read(RXBuffer, 0, RXBuffer.Length)
                    Res = Ascii.GetString(RXBuffer, 0, RXLenght)
                    Debug.WriteLine("FTP RX:" & Res)
                Catch
                    Res = ""
                    ConnectionOK = False
                    Debug.Write("FTP RX Timeout")
                End Try
            Else
                Debug.WriteLine("FTP RX(NOT-READY):" & Res)
            End If
        End If
        Return Res
    End Function

    Private Function WriteNet(Data As String) As Boolean
        Dim Res As Boolean = False
        If FTP_Stream IsNot Nothing Then
            If FTP_Stream.CanWrite Then
                Try
                    TXBuffer = Ascii.GetBytes(Data)
                    FTP_Stream.WriteAsync(TXBuffer, 0, TXBuffer.Length)
                    Res = True
                    Debug.WriteLine("FTP TX:" & Data)
                Catch ex As Exception
                    Res = False
                    Debug.WriteLine("FTP TX ERROR : " & ex.Message)
                End Try
            Else
                Debug.WriteLine("FTP TX(NOT-READY):" & Data)
            End If
        End If
        Return Res
    End Function

End Module



