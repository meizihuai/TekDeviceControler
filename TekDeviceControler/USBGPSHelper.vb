Imports System
Imports System.IO
Imports System.Text
Imports System.IO.Ports
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading
Imports System.Threading.Thread
Public Class USBGPSHelper
    Private flagIsPortOpen As Boolean = False
    Private openedPortName As String = ""
    Private mySerialPort As SerialPort
    Private gpsInfo As GPSInfo
    '  Private gpsInfoLock As Object
    Public Event Onlog(ByVal str As String)
    Public Event OnPortOpen()
    Public Event OnPortMsg(ByVal msg As String)
    Public Event Onlocation(ByVal lng As Double, ByVal lat As Double)
    Public Event OnRefrushGPSInfo(ByVal gpsInfo As GPSInfo)
    Public Event OnLocationed(ByVal bool As Boolean)
    Private isLocationed As Boolean = False
    Sub New()
        gpsInfo = New GPSInfo
        'gpsInfoLock = New Object
    End Sub

    Private Sub log(ByVal str As String)
        RaiseEvent Onlog(str)
    End Sub
    Private testCOMPortFlag As Boolean = False
    Public Sub Start()
        Dim th As New Thread(AddressOf RefrushPorts)
        th.Start()
    End Sub
    Private Sub RefrushPorts()
        While True
            Sleep(1000)
            log("正在扫描端口...")
            Try
                Dim portNames() As String = SerialPort.GetPortNames()
                If IsNothing(portNames) Then
                    log("没有端口！")
                    Continue While
                End If
                log("端口数量:" & portNames.Length)
                For Each itm In portNames
                    log("正在测试端口:" & itm)
                    testCOMPortFlag = False
                    CallWithTimeout(AddressOf TestCOMPort, itm, 3000)
                    Sleep(4000)
                    If testCOMPortFlag Then
                        log("端口:" & itm & "测试成功！")
                        RaiseEvent OnPortOpen()
                        Return
                    End If
                Next
            Catch ex As Exception

            End Try
        End While
    End Sub
    Private Sub CallWithTimeout(ByVal action As Action(Of String), ByVal portName As String, ByVal ms As Integer)
        Dim threadToKill As Thread = Nothing
        Dim wrappedAction As Action = New Action(Sub()
                                                     threadToKill = Thread.CurrentThread
                                                     action(portName)
                                                 End Sub)
        Dim result As IAsyncResult = wrappedAction.BeginInvoke(Nothing, Nothing)
        If result.AsyncWaitHandle.WaitOne(ms) Then
            wrappedAction.EndInvoke(result)
        Else
            threadToKill.Abort()
            Throw New TimeoutException()
        End If
    End Sub

    Private Sub TestCOMPort(ByVal portName As String)
        OpenPortByPortName(portName)
    End Sub
    Private Function OpenPortByPortName(ByVal portName As String) As Boolean
        Try
            CloseMyPort()
            mySerialPort = New SerialPort(portName)
            mySerialPort.BaudRate = 9600
            mySerialPort.Parity = Parity.None
            mySerialPort.StopBits = StopBits.One
            mySerialPort.DataBits = 8
            mySerialPort.Handshake = Handshake.None
            AddHandler mySerialPort.DataReceived, AddressOf DataReceivedHandler
            mySerialPort.Open()
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub DataReceivedHandler(ByVal sender As Object, ByVal e As SerialDataReceivedEventArgs)
        Dim sp As SerialPort = sender
        Dim msg As String = sp.ReadExisting()
        If msg = "" Then Return
        Dim sb() As String = msg.Split(vbCr)
        For Each itm In sb
            itm = itm.Replace(vbLf, "")
            HandleGPSMsg(itm)
        Next
    End Sub

    Private Sub CloseMyPort()
        If IsNothing(mySerialPort) = False Then
            If mySerialPort.IsOpen Then
                RemoveHandler mySerialPort.DataReceived, AddressOf DataReceivedHandler
                mySerialPort.Close()
            End If
        End If
    End Sub
    Private Sub HandleGPSMsg(ByVal msg As String)
        If msg = "" Then Return
        If msg.Length < 6 Then Return
        RaiseEvent OnPortMsg(msg)
        Dim func As String = msg.Substring(0, 6)
        msg = msg.Split("*")(0)
        Dim st() As String = msg.Split(",")
        func = st(0)
        If testCOMPortFlag = False Then
            If func.StartsWith("$G") Then
                testCOMPortFlag = True
            End If
        End If
        If func = "$GPGGA" Then '时间、位置、定位类型
            Dim GACount As String = st(7)
            gpsInfo.useGAScount = GACount
        End If
        If func = "$GPGLL" Then 'UTC时间、经度、纬度
            Dim lat As String = st(1)
            Dim NSLat As String = st(2)
            ' lblLat.Text = NSLat & "  " & lat
            Dim lng As String = st(3)
            Dim NSLng As String = st(4)
            '  lblLng.Text = NSLng & "  " & lng
            lat = ConvertDegreesToDigital(lat)
            lng = ConvertDegreesToDigital(lng)
            gpsInfo.lat = Val(lat)
            gpsInfo.lng = Val(lng)
            Dim UTC As String = st(5)
            If UTC <> "" Then
                Dim timeStr As String = Now.AddHours(-8).ToString("yyyy-MM-dd ") & UTC.Substring(0, 2) & ":" & UTC.Substring(2, 2) & ":" & UTC.Substring(4, 2)
                Try
                    Dim d As Date = Date.Parse(timeStr)
                    d = d.AddHours(8)
                    gpsInfo.time = d.ToString("yyyy-MM-dd HH:mm:ss")
                Catch ex As Exception

                End Try
            End If

            Dim isOK As String = st(6)
            If isOK = "A" Then
                gpsInfo.isLocationed = True
                If isLocationed = False Then
                    isLocationed = True
                    RaiseEvent OnLocationed(isLocationed)
                End If
                RaiseEvent Onlocation(Val(lng), Val(lat))
                'lblIsOk.ForeColor = Color.Green
                'Dim js As String = "theLocation"
                'script(js, New String() {lng, lat}.ToArray, Web)
                'script(js, New String() {lng, lat}.ToArray, Web)
            Else
                If isLocationed Then
                    isLocationed = False
                    RaiseEvent OnLocationed(isLocationed)
                End If
                gpsInfo.isLocationed = False
                ' lblIsOk.ForeColor = Color.Red
            End If
        End If
        If func = "$GPGSA" Then 'GPS接收机操作模式、定位使用的卫星、DOP值
            'Dim GPSMode As String = st(1)
            'If GPSMode = "A" Then
            '    lblGPSMode.Text = "自动"
            'Else
            '    lblGPSMode.Text = "手动"
            'End If
            Dim GPSMode2 As String = st(2)
            If GPSMode2 = "1" Then gpsInfo.GPSType = "定位无效"
            If GPSMode2 = "2" Then gpsInfo.GPSType = "2D定位"
            If GPSMode2 = "3" Then gpsInfo.GPSType = "3D定位"
            'Label12.Text = st(15)
            'Label14.Text = st(16)
            'Label16.Text = st(17)
        End If
        If func = "$GPGSV" Then '可见 GPS卫星信息、仰角、方位角、信噪比（SNR） RMC：时间、日期、位置、速 度
            '  lblUseGACount.Text = st(3)
            If st(3) = "" Then
                gpsInfo.useGAScount = 0
            Else
                gpsInfo.useGAScount = Val(st(3))
            End If
        End If
        RaiseEvent OnRefrushGPSInfo(gpsInfo)
    End Sub

    Private Function ConvertDegreesToDigital(ByVal degrees As String) As String
        If degrees = "" Then Return ""
        Dim value As Double = Convert.ToDouble(degrees)
        Dim ddmm As Integer = Math.Truncate(value)
        Dim mmmmmmm As Double = value - ddmm
        Dim dddmmDouble As Double = ddmm / 100
        Dim dd As Long = Math.Truncate(dddmmDouble)
        Dim mm As Long = (ddmm - dd * 100)
        Dim ss As Double = mmmmmmm * 60
        degrees = dd & "°" & mm & "′" & ss & "″"
        Dim num As Double = 60
        Dim digitalDegree As Double = 0.0
        Dim d As Integer = degrees.IndexOf("°")
        If d < 0 Then Return digitalDegree
        Dim degree As String = degrees.Substring(0, d)
        digitalDegree = digitalDegree + Convert.ToDouble(degree)
        Dim m As Integer = degrees.IndexOf("′")
        If m < 0 Then Return digitalDegree
        Dim minute As String = degrees.Substring(d + 1, m - d - 1)
        digitalDegree = digitalDegree + ((Convert.ToDouble(minute)) / num)
        Dim s As Integer = degrees.IndexOf("″")
        If s < 0 Then Return digitalDegree
        Dim second As String = degrees.Substring(m + 1, s - m - 1)
        digitalDegree = digitalDegree + (Convert.ToDouble(second) / (num * num))
        Return digitalDegree
    End Function
End Class
