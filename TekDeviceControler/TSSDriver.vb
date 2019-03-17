Imports System
Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Threading.Thread
Imports Newtonsoft
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Net
Imports System.Net.Sockets
Imports System.Runtime.InteropServices
Public Class TSSDriver
    Private DeviceId As String
    Private tcpcl As TcpClient
    Private runKind As String
    Public isConnected As Boolean = False
    Private connectLock As Object
    Private serverIP As String
    Private serverPort As Integer
    Public Event OnHandleTM(ByVal tm As tssMsg)
    Private reviceMsgThread As Thread
    Private heartBeatThread As Thread
    Sub New(ByVal _deviceId As String, ByVal ip As String, ByVal port As Integer, ByVal _runKind As String)
        connectLock = New Object
        DeviceId = _deviceId
        isConnected = False
        serverIP = ip
        serverPort = port
        runKind = _runKind
        inicrcTable()
    End Sub
    Public Sub Start()
        Dim th As New Thread(AddressOf ConnectToSever)
        th.Start()
    End Sub
    Private Sub HandleTm(ByVal tm As tssMsg)
        UnReciveTimes = 0
        RaiseEvent OnHandleTM(tm)
    End Sub

#Region "连接到TSS服务器"
    Private Sub ConnectToSever()
        Dim ipendpoint = New IPEndPoint(IPAddress.Parse(serverIP), serverPort)
        isConnected = False
        If IsNothing(tcpcl) = False Then
            If tcpcl.Connected Then
                Try
                    tcpcl.Close()
                Catch ex As Exception

                End Try
            End If
        End If
        SyncLock connectLock
            If isConnected Then Return
            While True
                Try
                    isConnected = False
                    tcpcl = New TcpClient
                    log("正在连接服务器……" & ipendpoint.ToString)
                    tcpcl.Connect(ipendpoint)
                    If tcpcl.Connected Then
                        isConnected = True
                        log("已连接服务器")
                        'If IsNothing(reviceMsgThread) = False Then
                        '    Try
                        '        reviceMsgThread.Abort()
                        '    Catch ex As Exception

                        '    End Try
                        'End If                     
                        reviceMsgThread = New Thread(AddressOf reciveMsg)
                        reviceMsgThread.Start()
                        'If IsNothing(heartBeatThread) = False Then
                        '    Try
                        '        heartBeatThread.Abort()
                        '    Catch ex As Exception

                        '    End Try
                        'End If
                        heartBeatThread = New Thread(AddressOf HeartBeat)
                        heartBeatThread.Start()
                        Sleep(100)
                        login()
                        Return
                    Else
                        isConnected = False
                    End If

                Catch ex As Exception
                    isConnected = False
                End Try
                Sleep(1000)
            End While
        End SyncLock
    End Sub
    Private Sub CloseReciveMsgThread()
        If IsNothing(reviceMsgThread) = False Then
            Try
                reviceMsgThread.Abort()
            Catch ex As Exception

            End Try
        End If
    End Sub
    Private Sub CloseHeartBeatThread()
        If IsNothing(heartBeatThread) = False Then
            Try
                heartBeatThread.Abort()
            Catch ex As Exception

            End Try
        End If
    End Sub
    Public Sub sendMsg2TSSServer(ByVal dataType As String, ByVal functionTye As String, ByVal canshuqu As String, ByVal shuju() As Byte)
        SendMsg(msg2TssMsg(&H0, dataType, functionTye, canshuqu, shuju))
    End Sub
    Private Sub login()
        sendMsg2TSSServer("link", "logon", runKind, Nothing)
        Sleep(1000)
        sendMsg2TSSServer("data", "version", myVerison, Nothing)
    End Sub
    Private Sub SendMsg(ByVal tm As tssMsg)
        Try
            'Console.WriteLine("发送  " & tm.datatype & "   " & tm.functype)
            If isConnected = False Then
                Return
            End If
            Dim by() As Byte = tssmsg2byte(tm)
            If IsNothing(by) Then
                log("TSSSer.SendData>ERR>Byte[]为空")
                Return
            End If
            tcpcl.GetStream.Write(by, 0, by.Length)
            tcpcl.GetStream.Flush()
        Catch ex As Exception
            log("SendMsg>ERR>" & ex.ToString)
            If InStr(ex.Message, "连接") Then
                CloseReciveMsgThread()
                CloseHeartBeatThread()
                Dim th As New Thread(AddressOf ConnectToSever)
                th.Start()
            End If
        End Try
    End Sub
    Dim byteSub() As Byte
    Private Sub HandleBuffer(ByVal buffer() As Byte)
        Try
            If IsNothing(byteSub) Then
                byteSub = buffer
            Else
                byteSub = byteSub.Concat(buffer).ToArray
            End If
            If byteSub.Length < 102 Then
                Exit Sub
            End If
            Dim readindex As Integer = 0
            Dim totalnum As Integer = byteSub.Length
            While True
                If readindex >= totalnum Then Exit While
                Try
                    If checkFlag(byteSub, readindex, totalnum - readindex) = True Then
                        byteSub = tobytes(byteSub, readindex, totalnum - readindex)
                        readindex = -1
                        totalnum = byteSub.Length
                        If byteSub.Length < 102 Then
                            Exit Sub
                        Else
                            Try
                                Dim tm As tssMsg = byte2tssmsgHead(byteSub)
                                If IsNothing(tm) = False Then
                                    Dim lenofmsg As Integer = tm.lenofmsg
                                    If byteSub.Length < lenofmsg Then
                                        Exit Sub
                                    End If
                                    If byteSub.Length = lenofmsg Then
                                        HandleTm(byte2tssmsg(byteSub))
                                        byteSub = Nothing
                                        Exit Sub
                                    End If
                                    If byteSub.Length > lenofmsg Then
                                        Dim bk() As Byte = tobytes(byteSub, 0, lenofmsg)
                                        HandleTm(byte2tssmsg(bk))
                                        byteSub = tobytes(byteSub, lenofmsg, byteSub.Length - lenofmsg)
                                        readindex = -1
                                        totalnum = byteSub.Count
                                    End If
                                Else
                                    byteSub = tobytes(byteSub, 102, byteSub.Length - 102)
                                    readindex = -1
                                    totalnum = byteSub.Count
                                End If
                            Catch ex As Exception

                            End Try
                        End If
                    End If
                Catch ex As Exception
                    '  'MsgBox("err2" & vbCrLf & ex.ToString)
                End Try
                readindex = readindex + 1
                If readindex >= totalnum Then Exit While
            End While
        Catch ex As Exception
            ' 'MsgBox("err3" & vbCrLf & ex.ToString)
        End Try
    End Sub
    Private Sub reciveMsg()
        While True
            Try
                Dim buffer(8192) As Byte
                Dim num As Integer = tcpcl.GetStream.Read(buffer, 0, buffer.Length)
                If num = 0 Then
                    CloseHeartBeatThread()
                    Dim th As New Thread(AddressOf ConnectToSever)
                    th.Start()
                    Return
                End If
                Try
                    Dim by(num - 1) As Byte
                    Array.Copy(buffer, 0, by, 0, by.Length)
                    HandleBuffer(by)
                Catch ex As Exception

                End Try
            Catch ex As Exception
                log("reciveMsg错误," & ex.Message)
                End
                CloseHeartBeatThread()
                Dim th As New Thread(AddressOf ConnectToSever)
                th.Start()
                Return
            End Try
        End While
    End Sub
    Private UnReciveTimes As Integer = 0
    Private Sub HeartBeat()
        Dim secend As Integer = 30 * 1000
        While True
            Try
                Sleep(secend)
                If isConnected = False Then
                    Return
                End If
                UnReciveTimes = UnReciveTimes + 1
                log("监测心跳包-->UnReciveTimes=" & UnReciveTimes)
                If UnReciveTimes >= 3 Then
                    UnReciveTimes = 0
                    isConnected = False
                    log("监测心跳包-->长时间没有接收到服务器消息，主动重新连接……")
                    CloseReciveMsgThread()

                    If IsNothing(tcpcl) = False Then
                        Try
                            If tcpcl.Connected Then
                                tcpcl.Close()
                            End If
                        Catch ex As Exception

                        End Try
                    End If
                    Dim th As New Thread(AddressOf ConnectToSever)
                    th.Start()
                    Return
                End If
                SendMsg(msg2TssMsg(&H0, "DevMsg", "Test", "", Nothing))
            Catch ex As Exception

            End Try
        End While
    End Sub
#End Region
#Region "通用处理程序"
    Private crcTable() As Byte
    Structure dataMsg
        Dim flag As String
        Dim username As String
        Dim pass As String
        Dim kind As String
        Dim group As String
        Dim tousername As String
        Dim togroup As String
        Dim func As String
        Dim msg As String
    End Structure
    Private Function ProxyConnect(ByVal proxyname As String, ByVal proxyport As Integer, ByVal remoteName As String, ByVal remotePort As Integer) As TcpClient
        Dim bool As Boolean = False
        Dim ipport1 As New IPEndPoint((Dns.GetHostByName(proxyname).AddressList(0)), proxyport)
        Dim tcpclient As TcpClient
        tcpclient = New TcpClient
        tcpclient.Connect(ipport1)
        If tcpclient.Connected Then
            Dim ns As NetworkStream = tcpclient.GetStream
            Dim by() As Byte = Encoding.Default.GetBytes("CONNECT " & Dns.GetHostByName(remoteName).AddressList(0).ToString & ":" & remotePort & vbCrLf & vbCrLf)
            ns.Write(by, 0, by.Length)
            ns.Flush()
            Dim bu(1024) As Byte
            Dim num As Integer = ns.Read(bu, 0, bu.Length)
            If num > 0 Then
                Dim str As String = Encoding.Default.GetString(bu, 0, num)
                If InStr(str, "200") Then
                    Return tcpclient
                End If
            Else
                Return Nothing
            End If
        End If
        Return Nothing
    End Function
    Private Function tobytes(ByVal by() As Byte, ByVal startindex As Integer, ByVal count As Integer) As Byte()
        If count <= 0 Then Return Nothing
        Dim bu(count - 1) As Byte
        If by.Count < count + startindex Then
            Return Nothing
        End If
        If by.Count - startindex >= count Then
            For i = 0 To count - 1
                bu(i) = by(startindex + i)
            Next
        End If
        Return bu
    End Function
    Private Function delchr(ByVal str As String) As String
        Dim St As String = ""
        St = str.Replace(ChrW(0), "")
        Return St
    End Function
    Private Function msgTodataMsg(ByVal username As String, ByVal pass As String, ByVal kind As String, ByVal group As String, ByVal tousername As String, ByVal togroup As String, ByVal func As String, ByVal msg As String) As dataMsg
        Dim dm As dataMsg
        dm.flag = "$radio"
        dm.username = username
        dm.pass = pass
        dm.kind = kind
        dm.group = group
        dm.tousername = tousername
        dm.togroup = togroup
        dm.func = func
        dm.msg = msg
        Return dm
    End Function
    Private Function BytesToDataMsg(ByVal bk() As Byte) As dataMsg
        If IsNothing(bk) Then Return Nothing
        If bk.Length < 15 Then Return Nothing
        Dim head As String = Encoding.Default.GetString(bk, 0, 6)
        If head <> "<head=" Then Return Nothing
        head = Encoding.Default.GetString(bk, 14, 1)
        If head <> ">" Then Return Nothing
        head = Encoding.Default.GetString(bk, 6, 8)
        Dim msgLength As Double = Val(head)
        If bk.Length - 15 = msgLength Then
            Dim txt As String = Encoding.Default.GetString(bk, 15, msgLength)
            Try
                Dim dm As dataMsg = JsonConvert.DeserializeObject(txt, GetType(dataMsg))
                Return dm
            Catch ex As Exception

            End Try
        End If
        Return Nothing
    End Function
    Private Function dataMsgToBytes(ByVal dm As dataMsg) As Byte()
        Dim txt As String = ""
        Try
            txt = JsonConvert.SerializeObject(dm)
            Dim by() As Byte = Encoding.Default.GetBytes(txt)
            Dim len As Double = by.Count
            Dim bk() As Byte = Encoding.Default.GetBytes("<head=")
            bk = bk.Concat(Encoding.Default.GetBytes(len.ToString("00000000"))).ToArray
            bk = bk.Concat(Encoding.Default.GetBytes(">")).ToArray
            bk = bk.Concat(by).ToArray
            Return bk
        Catch ex As Exception
            Return Nothing
        End Try
    End Function
    Private Function Transfor(ByVal by() As Byte) As Byte()
        Dim bk(by.Length - 1) As Byte
        For i = 0 To by.Length - 1
            bk(i) = Fa(by(i))
        Next
        Return bk
    End Function
    Private Function Fa(ByVal x As Byte) As Byte
        If 0 <= x And x <= 49 Then
            Return 49 - x
        End If
        If 50 <= x And x <= 99 Then
            Return 99 + 50 - x
        End If
        If 100 <= x And x <= 149 Then
            Return 100 + 149 - x
        End If
        If 150 <= x And x <= 199 Then
            Return 150 + 199 - x
        End If
        If 200 <= x And x <= 255 Then
            Return 200 + 255 - x
        End If
    End Function
    Private Function tostr(ByVal by() As Byte) As String
        If IsNothing(by) Then
            Return Nothing
        End If
        Dim str As String = Encoding.Default.GetString(by)
        Return str
    End Function
    Private Function tobyte(ByVal str As String) As Byte()
        If str = "" Then
            Return New Byte() {0}
        End If
        Dim by() As Byte = Encoding.Default.GetBytes(str)
        Return by
    End Function
    Private Function readfile(ByVal filename As String) As String
        If File.Exists(filename) = False Then Return Nothing
        Dim sr As New StreamReader(filename, Encoding.Default)
        Dim str As String = sr.ReadToEnd
        sr.Close()
        Return str
    End Function
    Private Function getHeadcrc(ByVal by() As Byte) As Byte
        Dim int As Byte = 0
        For i = 10 To 101
            Dim y As Integer = int Xor by(i)
            int = crcTable(y)
        Next
        Return int
    End Function
    Private Function checkFlag(ByVal by() As Byte, ByVal startindex As Integer, ByVal num As Integer) As Boolean
        If num > 9 Then num = 9
        Dim str As String = Mid("$RADIOINF", 1, num)
        Dim str2 As String = Encoding.Default.GetString(by, startindex, num)
        If str = str2 Then
            Return True
        Else
            Return False
        End If
    End Function
    Private Function tssmsg2byte(ByVal tm As tssMsg) As Byte()
        Dim by(101) As Byte
        Array.Copy(tobyte(tm.flag), by, tobyte(tm.flag).Count)
        Array.Copy(BitConverter.GetBytes(tm.lenofmsg + 1), 0, by, 10, 4)
        Array.Copy(New Byte() {Val(tm.ctrl)}, 0, by, 14, 1)
        Array.Copy(New Byte() {Val(tm.xieyibanbenhao)}, 0, by, 15, 1)
        Array.Copy(tobyte(tm.datatype), 0, by, 16, tobyte(tm.datatype).Length)
        Array.Copy(tobyte(tm.functype), 0, by, 32, tobyte(tm.functype).Length)
        Array.Copy(BitConverter.GetBytes(tm.baowenxuhao), 0, by, 48, 4)
        Array.Copy(BitConverter.GetBytes(tm.baotouchangdu), 0, by, 52, 2)
        Array.Copy(BitConverter.GetBytes(tm.canshuquchangdu), 0, by, 54, 2)
        Array.Copy(BitConverter.GetBytes(tm.shujuquchangdu), 0, by, 56, 4)
        '时标
        Dim de As Date = Now
        Dim yy As Integer = de.ToString("yy")
        Dim MM As Integer = de.ToString("MM")
        Dim dd As Integer = de.ToString("dd")
        Dim HH As Integer = de.ToString("HH")
        Dim m As Integer = de.ToString("mm")
        Dim ss As Integer = de.ToString("ss")
        Array.Copy(New Byte() {Format(yy, "00")}, 0, by, 60, 1)
        Array.Copy(New Byte() {Format(MM, "00")}, 0, by, 61, 1)
        Array.Copy(New Byte() {Format(dd, "00")}, 0, by, 62, 1)
        Array.Copy(New Byte() {Format(HH, "00")}, 0, by, 63, 1)
        Array.Copy(New Byte() {Format(m, "00")}, 0, by, 64, 1)
        Array.Copy(New Byte() {Format(ss, "00")}, 0, by, 65, 1)
        Dim ms As Short = 0
        Array.Copy(BitConverter.GetBytes(ms), 0, by, 66, 2)
        Array.Copy(BitConverter.GetBytes(ms), 0, by, 68, 2)
        Array.Copy(BitConverter.GetBytes(ms), 0, by, 70, 2)
        Array.Copy(tobyte(tm.deviceID), 0, by, 72, tobyte(tm.deviceID).Length)
        Array.Copy(tobyte(tm.source), 0, by, 82, tobyte(tm.source).Length)
        Array.Copy(tobyte(tm.destination), 0, by, 92, tobyte(tm.destination).Length)
        'CRC
        Dim crcInt As Byte = getHeadcrc(by)
        Array.Copy(New Byte() {crcInt}, 0, by, 9, 1)
        Dim bu() As Byte
        If tm.canshuquchangdu > 0 Then
            bu = tobyte(tm.canshuqu)
            ' Array.Copy(bu, 0, by, 102, tm.canshuquchangdu)
        End If
        Dim bk() As Byte
        If tm.shujuquchangdu > 0 Then
            bk = tm.shujuqu
            'Array.Copy(bk, 0, by, 102 + tm.canshuquchangdu, tm.shujuquchangdu)
        End If
        Dim k1 As Integer = 0
        Dim k2 As Integer = 0
        If IsNothing(bu) = False Then k1 = bu.Count
        If IsNothing(bk) = False Then k2 = bk.Count
        Dim bbb(101 + k1 + k2) As Byte
        Array.Copy(by, 0, bbb, 0, 102)
        If IsNothing(bu) = False Then Array.Copy(bu, 0, bbb, 102, k1)
        If IsNothing(bk) = False Then Array.Copy(bk, 0, bbb, 102 + k1, k2)
        Dim t As Integer = getcrc(bbb)
        Dim bkk() As Byte
        Dim bt(0) As Byte
        bt(0) = t
        bkk = bbb.Concat(bt).ToArray
        Return bkk
    End Function
    Private Function getcrc(ByVal by() As Byte) As Byte
        Dim int As Byte = 0
        For i = 0 To by.Length - 1
            Dim y As Integer = int Xor by(i)
            int = crcTable(y)
        Next
        Return int
    End Function
    Private Sub inicrcTable()
        crcTable = New Byte() {&H21, &H7F, &H9D, &HC3, &H40, &H1E, &HFC, &HA2, &HE3, &HBD, &H5F, &H1, &H82, &HDC, &H3E, &H60,
 &HBC, &HE2, &H0, &H5E, &HDD, &H83, &H61, &H3F, &H7E, &H20, &HC2, &H9C, &H1F, &H41, &HA3, &HFD,
 &H2, &H5C, &HBE, &HE0, &H63, &H3D, &HDF, &H81, &HC0, &H9E, &H7C, &H22, &HA1, &HFF, &H1D, &H43,
 &H9F, &HC1, &H23, &H7D, &HFE, &HA0, &H42, &H1C, &H5D, &H3, &HE1, &HBF, &H3C, &H62, &H80, &HDE,
 &H67, &H39, &HDB, &H85, &H6, &H58, &HBA, &HE4, &HA5, &HFB, &H19, &H47, &HC4, &H9A, &H78, &H26,
 &HFA, &HA4, &H46, &H18, &H9B, &HC5, &H27, &H79, &H38, &H66, &H84, &HDA, &H59, &H7, &HE5, &HBB,
 &H44, &H1A, &HF8, &HA6, &H25, &H7B, &H99, &HC7, &H86, &HD8, &H3A, &H64, &HE7, &HB9, &H5B, &H5,
 &HD9, &H87, &H65, &H3B, &HB8, &HE6, &H4, &H5A, &H1B, &H45, &HA7, &HF9, &H7A, &H24, &HC6, &H98,
 &HAD, &HF3, &H11, &H4F, &HCC, &H92, &H70, &H2E, &H6F, &H31, &HD3, &H8D, &HE, &H50, &HB2, &HEC,
 &H30, &H6E, &H8C, &HD2, &H51, &HF, &HED, &HB3, &HF2, &HAC, &H4E, &H10, &H93, &HCD, &H2F, &H71,
 &H8E, &HD0, &H32, &H6C, &HEF, &HB1, &H53, &HD, &H4C, &H12, &HF0, &HAE, &H2D, &H73, &H91, &HCF,
 &H13, &H4D, &HAF, &HF1, &H72, &H2C, &HCE, &H90, &HD1, &H8F, &H6D, &H33, &HB0, &HEE, &HC, &H52,
 &HEB, &HB5, &H57, &H9, &H8A, &HD4, &H36, &H68, &H29, &H77, &H95, &HCB, &H48, &H16, &HF4, &HAA,
 &H76, &H28, &HCA, &H94, &H17, &H49, &HAB, &HF5, &HB4, &HEA, &H8, &H56, &HD5, &H8B, &H69, &H37,
 &HC8, &H96, &H74, &H2A, &HA9, &HF7, &H15, &H4B, &HA, &H54, &HB6, &HE8, &H6B, &H35, &HD7, &H89,
 &H55, &HB, &HE9, &HB7, &H34, &H6A, &H88, &HD6, &H97, &HC9, &H2B, &H75, &HF6, &HA8, &H4A, &H14}
    End Sub
    Structure tssMsg
        Dim flag As String
        Dim crc As String
        Dim lenofmsg As Integer
        Dim ctrl As Byte
        Dim xieyibanbenhao As Byte
        Dim datatype As String
        Dim functype As String
        Dim baowenxuhao As Integer
        Dim baotouchangdu As Short
        Dim canshuquchangdu As Short
        Dim shujuquchangdu As Integer
        Dim shibiao As String
        Dim deviceID As String
        Dim source As String
        Dim destination As String
        Dim canshuqu As String
        Dim shujuqu As Byte()
    End Structure
    Private Function byte2tssmsg(ByVal by() As Byte) As tssMsg
        Dim tm As tssMsg
        tm.flag = delchr(Encoding.Default.GetString(by, 0, 9))
        tm.crc = by(9)
        tm.lenofmsg = BitConverter.ToInt32(by, 10)
        tm.ctrl = by(14)
        tm.xieyibanbenhao = by(15)
        tm.datatype = delchr(Encoding.Default.GetString(by, 16, 16))
        tm.functype = delchr(Encoding.Default.GetString(by, 32, 16))
        tm.baowenxuhao = BitConverter.ToInt32(by, 48)
        tm.baotouchangdu = BitConverter.ToInt16(by, 52)
        tm.canshuquchangdu = BitConverter.ToInt16(by, 54)
        tm.shujuquchangdu = BitConverter.ToInt32(by, 56)
        Try
            Dim shijian As String = ""
            shijian = "20" & by(60) & "-" & by(61) & "-" & by(62) & " " & by(63) & ":" & by(64) & ":" & by(65)
            tm.shibiao = Date.Parse(shijian).ToString("yyyy-MM-dd HH:mm:ss")
        Catch ex As Exception

        End Try
        tm.deviceID = delchr(Encoding.Default.GetString(by, 72, 10))
        tm.source = delchr(Encoding.Default.GetString(by, 82, 10))
        tm.destination = delchr(Encoding.Default.GetString(by, 82, 10))
        If tm.canshuquchangdu > 0 Then
            tm.canshuqu = delchr(Encoding.Default.GetString(by, 102, tm.canshuquchangdu))
        End If
        If tm.shujuquchangdu > 0 Then
            ReDim tm.shujuqu(tm.shujuquchangdu - 1)
            Array.Copy(by, 102 + tm.canshuquchangdu, tm.shujuqu, 0, tm.shujuquchangdu)
        End If
        Return tm
    End Function
    Private Function byte2tssmsgHead(ByVal by() As Byte) As tssMsg
        Dim tm As tssMsg
        tm.flag = delchr(Encoding.Default.GetString(by, 0, 9))
        tm.crc = by(9)
        tm.lenofmsg = BitConverter.ToInt32(by, 10)
        tm.ctrl = by(14)
        tm.xieyibanbenhao = by(15)
        tm.datatype = delchr(Encoding.Default.GetString(by, 16, 16))
        tm.functype = delchr(Encoding.Default.GetString(by, 32, 16))
        tm.baowenxuhao = BitConverter.ToInt32(by, 48)
        tm.baotouchangdu = BitConverter.ToInt16(by, 52)
        tm.canshuquchangdu = BitConverter.ToInt16(by, 54)
        tm.shujuquchangdu = BitConverter.ToInt32(by, 56)
        Try
            Dim shijian As String = ""
            shijian = "20" & by(60) & "-" & by(61) & "-" & by(62) & " " & by(63) & ":" & by(64) & ":" & by(65)
            tm.shibiao = Date.Parse(shijian).ToString("yyyy-MM-dd HH:mm:ss")
        Catch ex As Exception

        End Try
        tm.deviceID = delchr(Encoding.Default.GetString(by, 72, 10))
        tm.source = delchr(Encoding.Default.GetString(by, 82, 10))
        tm.destination = delchr(Encoding.Default.GetString(by, 82, 10))
        Return tm
    End Function



    Private Function msg2TssMsg(ByVal ctrl As Byte, ByVal datatype As String, ByVal functype As String, ByVal canshu As String, ByVal shuju() As Byte) As tssMsg
        Dim tm As New tssMsg
        tm.flag = "$RADIOINF"
        tm.ctrl = ctrl
        tm.datatype = datatype
        tm.functype = functype
        tm.canshuquchangdu = 0
        tm.shujuquchangdu = 0
        tm.deviceID = DeviceId
        Dim numofmsg As Integer = 102
        If canshu <> "" Then
            tm.canshuqu = canshu
            Dim b() As Byte = tobyte(tm.canshuqu)
            tm.canshuquchangdu = b.Count
            numofmsg = numofmsg + b.Count
        End If
        If IsNothing(shuju) = False Then
            ReDim tm.shujuqu(shuju.Count - 1)
            Array.Copy(shuju, tm.shujuqu, shuju.Count)
            tm.shujuquchangdu = shuju.Count
            numofmsg = numofmsg + shuju.Count
        End If
        tm.lenofmsg = numofmsg

        Return tm
    End Function
#End Region

End Class
