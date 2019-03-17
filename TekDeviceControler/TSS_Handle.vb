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
Imports TekDeviceControler.TSSDriver

Module TSS_Handle
    Public TD As TSSDriver
    Private runLocationLock As Object
    Public myRunLocation As runLocation
    Private gpsHelper As USBGPSHelper
    Public myRunLocationJson As String = ""
    Public Sub TSS_Handle_Start()
        TD = New TSSDriver(conf.deviceID, conf.serverIP, conf.serverPort, conf.runKind)
        AddHandler TD.OnHandleTM, AddressOf OnHandleTm
        TD.Start()
        If conf.runKind = "bus" Then
            runLocationLock = New Object
            OpenGPSHelper()
            'Dim th3 As New Thread(AddressOf RunLocationSimulate)
            'th3.Start()
        End If
    End Sub
    Private Sub OnHandleTm(ByVal tm As tssMsg)
        handleTM(tm)
    End Sub
    Private Sub OpenGPSHelper()
        gpsHelper = New USBGPSHelper
        AddHandler gpsHelper.Onlog, AddressOf log
        'AddHandler gpsHelper.OnPortOpen, AddressOf OnPortOpen
        'AddHandler gpsHelper.OnPortMsg, AddressOf OnPortMsg
        'AddHandler gpsHelper.Onlocation, AddressOf Onlocation
        AddHandler gpsHelper.OnLocationed, AddressOf OnLocationed
        AddHandler gpsHelper.OnRefrushGPSInfo, AddressOf OnRefrushGPSInfo
        gpsHelper.Start()
    End Sub
    Private Sub OnLocationed(ByVal bool As Boolean)
        If bool Then
            log("定位成功！")
        Else
            log("定位失败！")
        End If
    End Sub
    Private Sub OnRefrushGPSInfo(ByVal gpsInfo As GPSInfo)
        Dim localtionTime As String = Now.ToString("yyyy-MM-dd HH:mm:ss")
        SyncLock runLocationLock
            myRunLocation = New runLocation(gpsInfo.lng, gpsInfo.lat, localtionTime)
            myRunLocationJson = JsonConvert.SerializeObject(myRunLocation)
        End SyncLock
       
        'lblTime.Text = gpsInfo.time
        'lblLng.Text = gpsInfo.lng
        'lblLat.Text = gpsInfo.lat
        'Label24.Text = gpsInfo.ASL
        'Label26.Text = ""
        'lblIsOk.Text = gpsInfo.isLocationed
        'lblGPSMode.Text = gpsInfo.GPSType
        'lblGPSMode2.Text = ""
        'lblUnUseGACount.Text = gpsInfo.unUseGAScount
        'lblUseGACount.Text = gpsInfo.useGAScount

    End Sub
    Private Sub RunLocationSimulate()

        'Dim defaultLng As Single = 113.45678
        'Dim defaultLat As Single = 23.45669
        'Dim localtionLng As Single = defaultLng
        'Dim localtionLat As Single = defaultLat
        'Dim localtionTime As String = Now.ToString("yyyy-MM-dd HH:mm:ss")
        'Dim runTimes As Integer = 0
        'While True
        '    runTimes = runTimes + 1
        '    If runTimes >= 1000 Then
        '        runTimes = 0
        '        localtionLng = defaultLng
        '        localtionLat = defaultLat
        '    End If
        '    '  localtionLng = localtionLng + 0.001
        '    localtionLat = localtionLat + 0.001
        '    localtionTime = Now.ToString("yyyy-MM-dd HH:mm:ss")
        '    myRunLocation = New runLocation(localtionLng, localtionLat, localtionTime)
        '    myRunLocationJson = JsonConvert.SerializeObject(myRunLocation)
        '    'SendFreqSimulate()
        '    Sleep(1000)
        'End While
    End Sub
    Private Sub SendFreqSimulate()
        Dim tf As TekFreq
        tf.freqBegin = 88
        tf.freqStep = 25
        Dim yy(800) As Double
        For i = 0 To 800
            yy(i) = GetFreqValue()
        Next
        tf.yy = yy
        SendFreq(tf)
    End Sub
    Private Function GetFreqValue() As Double
        Dim min As Double = -100
        Dim max As Double = -70
        Dim v As Double = Rnd() * (max - min + 1) + min
        Return v
    End Function

    Structure runLocation
        Dim lng As String
        Dim lat As String
        Dim time As String
        Sub New(ByVal _lng As String, ByVal _lat As String, ByVal _time As String)
            lng = _lng
            lat = _lat
            time = _time
        End Sub
    End Structure


    Structure TekFreq
        Dim freqBegin As Double
        Dim freqStep As Double
        Dim xx() As Double
        Dim yy() As Double
    End Structure
    Public Sub SendFreq(ByVal tf As TekFreq)
        Try
            Dim freqbegin As Double = tf.freqBegin
            Dim freqStep As Double = tf.freqStep
            Dim xx() As Double = tf.xx
            Dim yy() As Double = tf.yy
            SendFreq(freqbegin, freqStep, xx, yy)
        Catch ex As Exception
            End
        End Try
    End Sub
    Public Sub SendFreq(ByVal freqbegin As Double, ByVal FreqStep As Double, ByVal xx() As Double, ByVal yy() As Double)
        FreqStep = FreqStep / 1000
        If TD.isConnected = False Then
            Return
        End If
        ' If xx.Length <> yy.Length Then Return
        Dim ppsj(yy.Length * 2 - 1) As Byte
        For i = 0 To yy.Length - 1
            Dim value As Double = yy(i) * 10
            Dim va As Int16 = Math.Floor(value)
            Dim by() As Byte = BitConverter.GetBytes(va)
            Array.Copy(by, 0, ppsj, i * 2, by.Length)
        Next
        Dim ResultBuffer(45 + ppsj.Length - 1) As Byte
        Array.Copy(BitConverter.GetBytes(CType(0, Int16)), 0, ResultBuffer, 0, 2)
        Array.Copy(BitConverter.GetBytes(CType(0, Int16)), 0, ResultBuffer, 2, 2)
        Array.Copy(BitConverter.GetBytes(CType(freqbegin, Double)), 0, ResultBuffer, 4, 8)
        Array.Copy(BitConverter.GetBytes(CType(FreqStep, Double)), 0, ResultBuffer, 12, 8)
        Array.Copy(BitConverter.GetBytes(CType(8000, Int32)), 0, ResultBuffer, 20, 4)
        Array.Copy(BitConverter.GetBytes(CType(0, Int16)), 0, ResultBuffer, 24, 2)
        Array.Copy(BitConverter.GetBytes(CType(0, Int16)), 0, ResultBuffer, 26, 2)
        Array.Copy(BitConverter.GetBytes(CType(0, Int16)), 0, ResultBuffer, 28, 2)
        ResultBuffer(30) = 16
        Array.Copy(BitConverter.GetBytes(CType(ppsj.Length, Int32)), 0, ResultBuffer, 37, 4)
        Array.Copy(BitConverter.GetBytes(CType(ppsj.Length, Int32)), 0, ResultBuffer, 41, 4)
        Array.Copy(ppsj, 0, ResultBuffer, 45, ppsj.Length)
        'MsgBox(ppsj.Length)
        If conf.runKind = "bus" Then
            SyncLock runLocationLock
                TD.sendMsg2TSSServer("spect_conti", "bscan", myRunLocationJson, ResultBuffer)
            End SyncLock
        Else
            TD.sendMsg2TSSServer("spect_conti", "bscan", "", ResultBuffer)
        End If

        '  log("ppsj")
    End Sub
    Public Sub SendAudio(ByVal buffer() As Byte, ByVal freq As Double)
        Try
            Dim dataLen As Integer = buffer.Length
            Dim ResultBuffer(dataLen + 43) As Byte
            Array.Copy(BitConverter.GetBytes(CType(freq / 1000, Double)), 0, ResultBuffer, 2, 8)
            Array.Copy(BitConverter.GetBytes(CType(32000, UInt32)), 0, ResultBuffer, 30, 4)
            Array.Copy(BitConverter.GetBytes(CType(dataLen, UInt32)), 0, ResultBuffer, 40, 4)
            ResultBuffer(34) = 16
            ResultBuffer(35) = 1
            Array.Copy(buffer, 0, ResultBuffer, 44, dataLen)
            TD.sendMsg2TSSServer("audio", "ifscan_wav", "", ResultBuffer)
        Catch ex As Exception
            ' log(ex.ToString)
        End Try
    End Sub


End Module
