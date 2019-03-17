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
Imports System.Media
Imports System.Net.NetworkInformation
Imports TekDeviceControler.TSSDriver
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.IO.Compression
Imports WavePlayer
Module Module1
    Public myVerison As String = "1.4.1.4"
    Public exeName As String = "TekDeviceControler.exe"
    Private TekDeviceId As String = ""
    Private flag_isConnected As Boolean = False
    Public device_func As String = ""
    Public device_freqStart As Double 'MHz
    Public device_freqStop As Double 'MHz
    Public device_freqStep As Double 'KHz
    Public conf As myConf
    Private flag_getData As Boolean = False
    Private thread_ReciveAudio As Thread
    Private thread_ReviceFreq As Thread
    Private flag_UploadFreq As Boolean = False
    Private flag_DownLoadData As Boolean = False
    Declare Sub mouse_event Lib "user32" Alias "mouse_event" (ByVal dwFlags As Integer, ByVal dx As Integer, ByVal dy As Integer, ByVal cButtons As Integer, ByVal dwExtraInfo As Integer)
    Declare Sub SetCursorPos Lib "user32" Alias "SetCursorPos" (ByVal X As Integer, ByVal Y As Integer)
    'errNotConnected=101
    Structure orderInfo
        Dim dataType As String
        Dim funcType As String
        Dim canshuqu As String
        Sub New(ByVal _dataType As String, ByVal _funcType As String, ByVal _canshuqu As String)
            dataType = _dataType
            funcType = _funcType
            canshuqu = _canshuqu
        End Sub
    End Structure
    Private Sub SaveOrder(ByVal dataType As String, ByVal funcType As String, ByVal canshuqu As String)
        Dim order As New orderInfo(dataType, funcType, canshuqu)
        Dim json As String = JsonConvert.SerializeObject(order)
        Dim path As String = "orderInfo.json"
        If File.Exists(path) Then File.Delete(path)
        Dim sw As New StreamWriter(path, False, Encoding.Default)
        sw.Write(json)
        sw.Close()
    End Sub
    Private Function ReadOrder() As orderInfo
        Dim path As String = "orderInfo.json"
        If File.Exists(path) = False Then Return Nothing
        Dim json As String = ""
        Dim sr As New StreamReader(path, Encoding.Default)
        json = sr.ReadToEnd
        sr.Close()
        Try
            Dim order As orderInfo = JsonConvert.DeserializeObject(json, GetType(orderInfo))
            Return order
        Catch ex As Exception
            Return Nothing
        End Try
    End Function
    Structure myConf
        Dim serverIP As String
        Dim serverPort As Integer
        Dim deviceID As String
        Dim runKind As String
        Dim httpPort As Integer
        Sub New(ByVal _serverIP As String, ByVal _serverPort As Integer, ByVal _deviceID As String, ByVal _runKind As String, ByVal _httpPort As Integer)
            serverIP = _serverIP
            serverPort = _serverPort
            deviceID = _deviceID
            runKind = _runKind
            httpPort = _httpPort
        End Sub
    End Structure


    Sub Main()
        Console.Title = "TekDeviceControler " & myVerison
        Dim ps() As Process = Process.GetProcesses()
        Dim count As Integer = 0
        For Each itm In ps
            If itm.ProcessName = exeName.Replace(".exe", "") Then
                count = count + 1
            End If
        Next
        If count > 1 Then
            End
        End If
        iniConf()
        myselini()
        TSS_Handle_Start()
        Dim MainHttpListenerUrl As String = "http://+:" & conf.httpPort & "/watchdog/"
        Dim m As New MainHttpListener(MainHttpListenerUrl)
        m.Start()
        Module1.ini()
        Console.ReadLine()
    End Sub
    Private Sub myselini()
        Try
            log("该程序版本号：" & myVerison)
            log("本设备ID:" & conf.deviceID)
            log("正在自检……")
            Dim p As New Ping
            Dim ps As PingReply = p.Send("123.207.31.37")
            If ps.Status <> IPStatus.Success Then
                Return
            End If
            CheckLocationDllFile("Microsoft.VisualBasic.PowerPacks.Vs.dll")
            CheckLocationDllFile("Newtonsoft.Json.dll")
            CheckLocationDllFile("update.exe")
            CheckLocationDllFile("进程守护.exe")
            log("正在检查更新……")
            HandleUpdate()
        Catch ex As Exception

        End Try
    End Sub
    Private Sub iniConf()
        Dim path As String = Directory.GetCurrentDirectory() & "\" & "config.ini"
        If File.Exists(path) Then
            Dim sr As New StreamReader(path, Encoding.Default)
            Dim json As String = sr.ReadToEnd
            sr.Close()
            Try
                conf = JsonConvert.DeserializeObject(json, GetType(myConf))
                If conf.httpPort = 0 Then
                    conf.httpPort = 3210
                End If
                Return
            Catch ex As Exception

            End Try
        End If
        conf = New myConf("123.207.31.37", 3204, "Tek_Test_01", "", 3210)
        Dim js As String = JsonConvert.SerializeObject(conf)
        Dim sw As New StreamWriter(path, False, Encoding.Default)
        sw.Write(js)
        sw.Close()
    End Sub
    Private isConnectingDevice As Boolean = False
    Public Sub ini()
        If flag_isConnected Then Return
        If isConnectingDevice Then
            Return
        Else
            isConnectingDevice = True
        End If
        Dim th As New Thread(AddressOf BusFreqOrder)
        th.Start()
        If conf.runKind = "bus" Then

        End If

        log("搜索设备……")
        While True
            Dim DeviceID As String = SearchDevice()
            TekDeviceId = DeviceID
            If DeviceID <> "" Then
                log("搜索到设备：" & DeviceID)
                If DeviceReset(DeviceID) = False Then
                    ini()
                    Return
                End If

                If Connect2Device(DeviceID) Then
                    flag_isConnected = True
                    isConnectingDevice = False
                    If device_freqStart = 0 Or device_freqStep = 0 Or device_freqStop = 0 Then
                        If conf.runKind = "bus" Then
                            order_freq(30, 6000, 25, True)

                        Else
                            Dim order As orderInfo = ReadOrder()
                            If IsNothing(order) Then
                                log("orderInfo=null")
                                order_freq(88, 108, 25, True)
                            Else
                                If order.dataType <> "" And order.funcType <> "" Then
                                    If order.dataType = "task" And order.funcType = "taskctrl" And order.canshuqu = "<taskctrl:taskstate=stop;>" Then
                                        log("orderInfo=stop")
                                        'order_freq(88, 108, 25, True)
                                        Dim tm As New tssMsg
                                        tm.datatype = order.dataType
                                        tm.functype = order.funcType
                                        tm.canshuqu = order.canshuqu
                                        handleTM(tm)
                                    Else
                                        log("orderInfo=good")
                                        Dim tm As New tssMsg
                                        tm.datatype = order.dataType
                                        tm.functype = order.funcType
                                        tm.canshuqu = order.canshuqu
                                        handleTM(tm)
                                    End If
                                Else
                                    log("orderInfo=null")
                                    order_freq(88, 108, 25, True)
                                End If
                            End If
                            'Dim freq As Double = 94.7
                            'order_audio(freq, freq, 25, freq)
                        End If
                        'order_freq(400, 500, 25)
                    Else
                        order_freq(device_freqStart, device_freqStop, device_freqStep, True)
                    End If
                    'order_audio(88, 108, 25, 90.6)
                    Exit While
                Else
                    flag_isConnected = False
                    log("连接设备失败，重新启动本控制台")
                    ReStartRun()
                End If
            End If
            Sleep(1000)
        End While
    End Sub
    Private Sub BusFreqOrder()
        log("已启动上传数据保护机制")
        While True
            Sleep(50000)
            If flag_UploadFreq = False Then
                log("监测到本系统已停止传送数据，主动请求88-108频谱一次……")
                Dim th As New Thread(AddressOf freqDataTest)
                th.Start()
                Dim count As Integer = 0
                While count < 10
                    count = count + 1
                    log("已等待" & count & "秒,flag_UploadFreq=" & flag_UploadFreq)
                    If flag_UploadFreq Then
                        Exit While
                    End If
                    Sleep(1000)
                End While
                If flag_UploadFreq Then
                    log("设备数据正常")
                Else
                    log("设备无法获取数据，主动重新启动控制台")
                    ReStartRun()
                End If
            Else
                log("设备上传数据正常")
            End If
            flag_UploadFreq = False
        End While
    End Sub
    Private Function freqDataTest() As Boolean
        'Dim tmpid As Integer = 0
        'Try
        '    If TekDeviceId = "" Then
        '        tmpid = 0
        '    End If
        '    If IsNumeric(TekDeviceId) = False Then
        '        tmpid = 0
        '    End If
        '    tmpid = Val(TekDeviceId)
        'Catch ex As Exception
        '    tmpid = 0
        'End Try
        'log("重置设备,tmpid=" & tmpid)
        'Dim itmp As IntPtr = DEVICE_Reset(tmpid)
        'If itmp <> 0 Then
        '    log("重置失败，设备已断开，itmp=" & itmp.ToString)
        '    Return False
        'End If
        'Dim itmp As IntPtr = DEVICE_PrepareForRun()
        'log("itmp=" & itmp.ToString & "," & Code2Msg(GetErrorString(itmp)))
        'If itmp.ToString <> "0" Then
        '    Return False
        'End If
        Try

       
        Dim freqStart As Double = 88
        Dim freqEnd As Double = 108
        Dim freqStep As Double = 25
        freqStart = freqStart * 1000000
        freqEnd = freqEnd * 1000000
        freqStep = freqStep * 1000
        Dim freqCenter As Double = (freqStart + freqEnd) / 2
        Dim traceLength As Integer = (freqEnd - freqStart) / freqStep
        traceLength = traceLength + 1
        SPECTRUM_SetEnable(True)
        CONFIG_SetCenterFreq(freqCenter)
        'If Spec_SetEnable(True) = False Then Return False
        'log("spec_setenable")
        'If SetCenterFreq(freqCenter) = False Then Return False
        'log("setcenterfreq")
        Dim setting As Spectrum_Settings
        setting.span = freqEnd - freqStart
        setting.rbw = 1000
        setting.enableVBW = False
        setting.vbw = 200000
        setting.traceLength = GetTraceLength(freqStart, freqEnd, freqStep)
        setting.window = 0
        setting.verticalUnit = 0
        SPECTRUM_SetSettings(setting)
        ' If SetFreqPara(setting) = False Then Return False
        AUDIO_Stop()
        DEVICE_Run()
        ' log("audio_stop")
        'If DeviceRun() = False Then Return False
        ' log("devicerun")


        Dim maxTracePoints As Integer = setting.traceLength
        Dim int As Integer = 0
        Dim size As Integer = maxTracePoints * Marshal.SizeOf(GetType(Single))
        While True
            int = int + 1
            If int >= 10 Then
                Return False
            End If
            If SPECTRUM_AcquireTrace().ToString <> 0 Then
                log("SPECTRUM_AcquireTrace!=0")
                Return False
            End If
            Dim r As Boolean = False
            Dim it As IntPtr = SPECTRUM_WaitForTraceReady(1000, r)
            If it <> 0 Then
                log("SPECTRUM_WaitForTraceReady=" & it.ToInt32)
                Sleep(1000)
                Continue While
            End If
            If r = False Then
                log("SPECTRUM_WaitForTraceReady=false")
                Sleep(1000)
                Continue While
            End If
            Dim traceData As IntPtr = Marshal.AllocHGlobal(Size)
            Dim outTracePoints As Integer = 0
            Dim trace As Int32 = 0
            Dim i As IntPtr = SPECTRUM_GetTrace(trace, maxTracePoints, traceData, outTracePoints)
            If i = 101 Then
                log("接收频谱数据-->ERR-->" & "设备已断开")
                Return False
            End If
            If i <> 0 Then Return False
            Dim by(Size - 1) As Byte
            Marshal.Copy(traceData, by, 0, by.Length)
            Dim ik As Integer = 0
            Dim yy(maxTracePoints - 1) As Double
            For m = 0 To by.Length - 1 Step 4
                Dim k1 As Single = BitConverter.ToSingle(by, m)
                yy(ik) = k1
                ik = ik + 1
            Next
            For t = 0 To 10
                log(yy(t))
            Next
            log("收到设备PPSJ, Point=" & yy.Length, False)
            flag_UploadFreq = True
            DEVICE_Stop()
            Return True
        End While
        Catch ex As Exception
            Return False
        End Try
        Return False
    End Function
    Public flagNeedRestart As Boolean = False
    Private Sub ReStartRun()
        Try
            flagNeedRestart = True
            Dim path As String = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
            Dim p As New Process
            p.StartInfo.FileName = path
            p.Start()
            End
            Return
        Catch ex As Exception

        End Try
        End
    End Sub
    Public Function Compress(ByVal data() As Byte) As Byte()
        Dim stream As MemoryStream = New MemoryStream
        Dim gZip As New GZipStream(stream, CompressionMode.Compress)
        gZip.Write(data, 0, data.Length)
        gZip.Close()
        Return stream.ToArray
    End Function
    Public Function Decompress(ByVal data() As Byte) As Byte()
        Dim stream As MemoryStream = New MemoryStream
        Dim gZip As New GZipStream(New MemoryStream(data), CompressionMode.Decompress)
        Dim n As Integer = 0
        While True
            Dim by(409600) As Byte
            n = gZip.Read(by, 0, by.Length)
            If n = 0 Then Exit While
            stream.Write(by, 0, n)
        End While
        gZip.Close()
        Return stream.ToArray
    End Function
    Structure MouseClickInfo
        Dim type As Integer
        Dim x As Integer
        Dim y As Integer
    End Structure
    Private Sub SendScreenImage()
        Dim bmp As Bitmap = GetScreenImage()
        If IsNothing(bmp) Then Return
        Dim by() As Byte = img2data(bmp)
        If IsNothing(by) Then Return
        Dim comby() As Byte = Compress(by)
        If IsNothing(comby) Then Return
        TD.sendMsg2TSSServer("data", "ScreenImage", "", comby)
    End Sub
    Public Sub handleTM(ByVal tm As tssMsg)
        If IsNothing(tm) Then Exit Sub
        flag_DownLoadData = True
        If tm.datatype = "DevMsg" And tm.functype = "Test" Then
            log("收到服务器心跳包")

        End If
        'log("收到命令:" & buffer.Length & "字节")
        'log("dataType=" & tm.datatype)
        'log("funcType=" & tm.functype)
        'log("canshuqu=" & tm.canshuqu)
        If tm.datatype = "task" And tm.functype = "reboot" Then
            log("收到重启控制程序命令，本软件会重新启动")
            ReStartRun()
        End If
        If tm.datatype = "task" And tm.functype = "GetScreenImage" Then
            log("接收到上传控制系统桌面命令……")
            Dim bmp As Bitmap = GetScreenImage()
            If IsNothing(bmp) Then Return
            Dim by() As Byte = img2data(bmp)
            If IsNothing(by) Then Return
            Dim comby() As Byte = Compress(by)
            If IsNothing(comby) Then Return
            TD.sendMsg2TSSServer("data", "ScreenImage", "", comby)
            log("已发送桌面截图，原始图片小=" & GetLen(by.Length) & "，压缩后大小=" & GetLen(comby.Length))
        End If
        If tm.datatype = "task" And tm.functype = "ReStartWindows" Then
            ReStartWindows()
        End If
        If tm.datatype = "task" And tm.functype = "ShutdownWindows" Then
            ShutdownWindows()
        End If
        If tm.datatype = "task" And tm.functype = "mouseClick" Then
            Try
                Dim mcInfo As MouseClickInfo = JsonConvert.DeserializeObject(tm.canshuqu, GetType(MouseClickInfo))
                If IsNothing(mcInfo) = False Then
                    If mcInfo.type = 1 Then
                        click_left(mcInfo.x, mcInfo.y)
                    End If
                    If mcInfo.type = 2 Then
                        click_right(mcInfo.x, mcInfo.y)
                    End If
                End If
            Catch ex As Exception

            End Try

        End If

        If tm.datatype = "DevMsg" And tm.functype = "Test" And tm.canshuqu = "<info:func=;>" Then
            ' log("收到心跳命令")
            TD.sendMsg2TSSServer("DevMsg", "Test", "", Nothing)
        End If
        If tm.datatype = "task" And tm.functype = "bscan" Then   '频谱

            Sleep(1000)
            Dim str As String = tm.canshuqu
            Dim freqbegin As String = getValueByCanShuQu(str, "freqbegin")
            Dim freqend As String = getValueByCanShuQu(str, "freqend")
            Dim freqstep As String = getValueByCanShuQu(str, "freqstep")
            Dim Bscan_freqBegin As Double = Val(freqbegin)
            Dim Bscan_freqEnd As Double = Val(freqend)
            Dim Bscan_freqStep As Double = Val(freqstep)
            SaveOrder(tm.datatype, tm.functype, tm.canshuqu)
            If flag_isConnected = False Then
                log("收到频谱命令,但是设备未连接，命令没有下达")
                Return
            End If
            DeviceStop()
            log("收到频谱命令，Bscan_freqBegin=" & Bscan_freqBegin & ",Bscan_freqEnd=" & Bscan_freqEnd & ",Bscan_freqStep=" & Bscan_freqStep)
            order_freq(Bscan_freqBegin, Bscan_freqEnd, Bscan_freqStep, True)
        End If
        If tm.datatype = "task" And tm.functype = "ifscan_wav" Then
          



            Sleep(1000)
            Dim str As String = tm.canshuqu
            Dim freqbegin As String = 88
            Dim freqend As String = 108
            Dim freqstep As String = 25
            Dim freq As String = getValueByCanShuQu(str, "freq")
            Dim Bscan_freqBegin As Double = Val(freqbegin)
            Dim Bscan_freqEnd As Double = Val(freqend)
            Dim Bscan_freqStep As Double = Val(freqstep)
            SaveOrder(tm.datatype, tm.functype, tm.canshuqu)
            If flag_isConnected = False Then
                log("收到音频命令,但是设备未连接，命令没有下达")
                Return
            End If
            DeviceStop()
            log("收到音频命令，freq=" & freq & ",Bscan_freqBegin=" & Bscan_freqBegin & ",Bscan_freqEnd=" & Bscan_freqEnd & ",Bscan_freqStep=" & Bscan_freqStep)
            order_audio(Bscan_freqBegin, Bscan_freqEnd, Bscan_freqStep, freq)

        End If
        If tm.datatype = "task" And tm.functype = "taskctrl" And tm.canshuqu = "<taskctrl:taskstate=stop;>" Then
            SaveOrder(tm.datatype, tm.functype, tm.canshuqu)
            If flag_isConnected = False Then
                log("收到停止命令,但是设备未连接，命令没有下达")
                Return
            End If
            log("收到设备停止命令")
            DeviceStop()         
        End If
    End Sub
    Public Sub order_audio(ByVal freqStart As Double, ByVal freqEnd As Double, ByVal freqStep As Double, ByVal freq As Double)
        freqStart = freqStart * 1000000
        freqEnd = freqEnd * 1000000
        freqStep = freqStep * 1000
        freq = freq * 1000000
        Dim freqCenter As Double = (freqStart + freqEnd) / 2
        Dim traceLength As Integer = (freqEnd - freqStart) / freqStep
        traceLength = traceLength + 1
        If Spec_SetEnable(True) = False Then Return
        If SetCenterFreq(freqCenter) = False Then Return
        'Dim setting As Spectrum_Settings
        'setting.span = freqEnd - freqStart
        'setting.rbw = 1000
        'setting.enableVBW = False
        'setting.vbw = 300000
        'setting.traceLength = GetTraceLength(freqStart, freqEnd, freqStep)
        'setting.window = 0
        'setting.verticalUnit = 0
        'SPECTRUM_SetSettings(setting)
        'setting = GetFreqPara()
        'If IsNothing(setting) Then Return
        'device_freqStart = setting.actualStartFreq / 1000000
        'device_freqStop = setting.actualStopFreq / 1000000
        'device_freqStep = setting.actualFreqStepSize / 1000
        'device_freqStart = freqStart
        'device_freqStop = freqEnd
        'device_freqStep = freqStep
        ''Dim maxTracePoints As Integer = setting.traceLength
        If SetCenterFreq(freq) = False Then Return
        Dim k As AudioDemodMode = AudioDemodMode.ADM_FM_200KHZ
        AUDIO_SetMode(k)
        AUDIO_SetFrequencyOffset(0)
        AUDIO_SetMute(True)
        AUDIO_SetVolume(1)
        Dim enable As Boolean = False
        AUDIO_GetEnable(enable)
        log("AUDIO_GetEnable=" & enable)
        'If enable = False Then Return
        If DeviceRun() = False Then Return
        AUDIO_Start()
        device_func = "audio"
        log("开始接收音频数据")

        flag_getData = True
        If IsNothing(thread_ReciveAudio) = False Then
            Try
                thread_ReciveAudio.Abort()
            Catch ex As Exception

            End Try
        End If

        thread_ReciveAudio = New Thread(AddressOf ReciveAudioSub)
        thread_ReciveAudio.Start(freq)
    End Sub
    Private Sub ReciveAudioSub(ByVal freq As Double)
        Dim inSize As UInt16 = 16384
        'inSize = 12000
        Dim outSize As UInt16 = 0
        Dim size As Integer = inSize * Marshal.SizeOf(GetType(Int16))
        Dim buffer() As Byte
        Dim count As Integer = 0
        flag_getData = True
        While True
            Try
                If flag_isConnected = False Then
                    ini()
                    Return
                End If

                If flag_getData = False Then Return
                Sleep(500)
                'count = count + 1
                'If count = 20 Then
                '    Dim path As String = "a.wav"
                '    If File.Exists(path) Then File.Delete(path)
                '    Dim fs As New FileStream(path, FileMode.OpenOrCreate)
                '    Dim bw As New BinaryWriter(fs)
                '    ' bw.Write(addWavHead(buffer, 16000, 16, 2))
                '    bw.Write(buffer)
                '    bw.Close()
                '    fs.Close()

                '    log("saved")


                '    ' MsgBox("done")

                '    ' Return
                'End If
                If flag_getData = False Then Return
                Dim pt As IntPtr = Marshal.AllocHGlobal(size)
                ' log("AUDIO_GetDataing……")
                Dim flag_audio_enable As Boolean = False
                Dim it As IntPtr = AUDIO_GetEnable(flag_audio_enable)
                log("flag_audio_enable:" & flag_audio_enable)
                ' log("AUDIO_GetData:" & Code2Msg(GetErrorString(it)))
                AUDIO_GetData(pt, inSize, outSize)
                log("outSize=" & outSize)
                If outSize = 0 Then Sleep(1000) : Continue While
                Dim by(outSize * 2 - 1) As Byte
                Marshal.Copy(pt, by, 0, by.Length)
                'If IsNothing(buffer) Then
                '    buffer = by
                'Else
                '    buffer = buffer.Concat(by).ToArray
                'End If
                flag_UploadFreq = True
                If IsNothing(by) = False Then

                    'play(addWavHead(by, 32000, 16, 1))
                    'If True Then
                    '    Dim objWavePlayer As New WavePlayer.WavePlayer(16000, 176400, 2, 16)
                    '    objWavePlayer.Load(by, 0)
                    '    objWavePlayer.Play()
                    'End If
                    SendAudio(by, freq / 1000)
                    log("SendAudio  by.length=" & by.Length & "   " & GetLen(by.Length))

                End If
                ' Dim wavByte() As Byte = addWavHead(by, 16000, 16, 2)
                ' play(wavByte)
            Catch ex As Exception
                log("接收音频数据-->ERR-->" & ex.ToString)
            End Try

            'log("wavByte.length=" & wavByte.Length)

        End While
    End Sub
    Private Function GetLen(ByVal length As Long) As String
        If length < 1024 Then Return length & " b"
        If length < 1024 * 1024 Then
            Dim d As Double = length / 1024
            Return d.ToString("0.00") & " kb"
        End If
        Dim dd As Double = length / (1024 * 1024)
        Return dd.ToString("0.00") & " mb"
    End Function
    Private Sub play(ByVal buf() As Byte)
        Try
            Dim ms As MemoryStream = New MemoryStream(buf)
            Dim sp As SoundPlayer = New SoundPlayer(ms)
            sp.Play()
            'If IsNothing(wavlistObject) Then
            '    wavlistObject = New Object
            'End If
            'SyncLock wavlistObject
            '    If IsNothing(wavlist) Then
            '        wavlist = New List(Of Byte())
            '    End If
            '    If IsNothing(buf) Then Exit Sub
            '    wavlist.Add(buf)
            'End SyncLock
        Catch ex As Exception
            log(ex.Message)
            'MsgBox(ex.ToString)
        End Try
    End Sub
    Public Function addWavHead(ByVal by() As Byte, ByVal caiyanglv As Integer, ByVal weishu As Integer, ByVal tongdaoshu As Integer) As Byte()
        Dim bu(43) As Byte
        Array.Copy(Encoding.Default.GetBytes("RIFF"), 0, bu, 0, 4)            '标志
        Array.Copy(BitConverter.GetBytes(by.Count - 8 + 44), 0, bu, 4, 4)     'by.count-8+44
        Array.Copy(Encoding.Default.GetBytes("WAVE"), 0, bu, 8, 4)            'WAVE
        Array.Copy(Encoding.Default.GetBytes("fmt "), 0, bu, 12, 4)           'FMT 
        Array.Copy(BitConverter.GetBytes(16), 0, bu, 16, 4)                   '16
        Dim s As Short = 1
        Array.Copy(BitConverter.GetBytes(s), 0, bu, 20, 2)                    '1为线性PCM编码，>1为压缩
        Array.Copy(BitConverter.GetBytes(tongdaoshu), 0, bu, 22, 2)           '声道数
        Array.Copy(BitConverter.GetBytes(caiyanglv), 0, bu, 24, 4)                 '采样率
        Dim int As Integer = caiyanglv * tongdaoshu * weishu / 8
        Array.Copy(BitConverter.GetBytes(int), 0, bu, 28, 4)                 '采样率*通道数*位数/8
        s = tongdaoshu * weishu / 8
        Array.Copy(BitConverter.GetBytes(s), 0, bu, 32, 2)                    '通道数*位数/8
        s = weishu
        Array.Copy(BitConverter.GetBytes(s), 0, bu, 34, 2)
        Array.Copy(Encoding.Default.GetBytes("data"), 0, bu, 36, 4)
        Array.Copy(BitConverter.GetBytes(by.Count), 0, bu, 40, 4)
        Dim bk(43 + by.Count) As Byte
        Array.Copy(bu, 0, bk, 0, bu.Count)
        Array.Copy(by, 0, bk, 44, by.Count)
        Return bk
    End Function
    Private Function GetTraceLength(ByVal freqStart As Double, ByVal freqEnd As Double, ByVal freqStep As Double) As Long
        Dim realCount As Long = (freqEnd - freqStart) / freqStep
        realCount = realCount + 1
        If realCount <= 801 Then Return 801
        If realCount <= 2401 Then Return 2401
        If realCount <= 4001 Then Return 4001
        If realCount <= 8001 Then Return 8001
        If realCount <= 10401 Then Return 10401
        If realCount <= 16001 Then Return 16001
        If realCount <= 32001 Then Return 32001
        Return 64001
    End Function
    Public Sub order_freq(ByVal freqStart As Double, ByVal freqEnd As Double, ByVal freqStep As Double, ByVal openThread As Boolean)
        freqStart = freqStart * 1000000
        freqEnd = freqEnd * 1000000
        freqStep = freqStep * 1000
        Dim freqCenter As Double = (freqStart + freqEnd) / 2
        Dim traceLength As Integer = (freqEnd - freqStart) / freqStep
        traceLength = traceLength + 1
        If Spec_SetEnable(True) = False Then Return
        log("spec_setenable")
        If SetCenterFreq(freqCenter) = False Then Return
        log("setcenterfreq")
        Dim setting As Spectrum_Settings
        setting.span = freqEnd - freqStart
        setting.rbw = 1000
        setting.enableVBW = False
        setting.vbw = 200000
        setting.traceLength = GetTraceLength(freqStart, freqEnd, freqStep)
        setting.window = 0
        setting.verticalUnit = 0
        If SetFreqPara(setting) = False Then Return
        log("setfreqpara")
        setting = GetFreqPara()
        log("getfreqpara")
        If IsNothing(setting) Then Return
        device_freqStart = setting.actualStartFreq / 1000000
        device_freqStop = setting.actualStopFreq / 1000000
        device_freqStep = setting.actualFreqStepSize / 1000
        AUDIO_Stop()
        log("audio_stop")
        If DeviceRun() = False Then Return
        log("devicerun")
        device_func = "bscan"
        Dim maxTracePoints As Integer = setting.traceLength
        Dim xx(setting.traceLength - 1) As Double
        For m = 0 To xx.Length - 1
            xx(m) = setting.actualStartFreq + m * setting.actualFreqStepSize
        Next
        flag_getData = True
        Dim stu As New FreqSubStu(xx, maxTracePoints)
        log("开始接收频谱数据")
        If openThread Then
            If IsNothing(thread_ReviceFreq) = False Then
                Try
                    thread_ReviceFreq.Abort()
                Catch ex As Exception

                End Try
            End If
            thread_ReviceFreq = New Thread(AddressOf ReciveFreqSub)
            thread_ReviceFreq.Start(stu)
        End If

    End Sub

    Structure FreqSubStu
        Dim xx() As Double
        Dim maxTracePoints As Integer
        Sub New(ByVal _xx() As Double, ByVal _maxTracePoints As Integer)
            xx = _xx
            maxTracePoints = _maxTracePoints
        End Sub
    End Structure
    Private Sub ReciveFreqSub(ByVal stu As FreqSubStu)
        If IsNothing(stu) Then Return
        If IsNothing(stu.xx) Then Return
        If stu.maxTracePoints <= 0 Then Return
        Dim maxTracePoints As Integer = stu.maxTracePoints
        Dim xx() As Double = stu.xx
        Dim size As Integer = maxTracePoints * Marshal.SizeOf(GetType(Single))
        flag_getData = True
        Dim oldTime As Date = Now
        Dim int As Integer = 1
        While True
            If flag_isConnected = False Then
                ini()
                Return
            End If
            If flag_getData = False Then Return
            Try
                If Spec_AcquireTrace() = False Then Return
                If spec_WaitForTraceReady() = False Then
                    log("spec_WaitForTraceReady=" & False)
                    Continue While
                End If

                Dim traceData As IntPtr = Marshal.AllocHGlobal(size)
                Dim outTracePoints As Integer = 0
                Dim trace As Int32 = 0
                Dim i As IntPtr = SPECTRUM_GetTrace(trace, maxTracePoints, traceData, outTracePoints)
                If i = 101 Then
                    log("接收频谱数据-->ERR-->" & "设备已断开")
                    flag_isConnected = False
                    isConnectingDevice = False
                    ini()
                    Return
                End If
                If i <> 0 Then Return
                Dim by(size - 1) As Byte
                Marshal.Copy(traceData, by, 0, by.Length)
                Dim ik As Integer = 0
                Dim yy(xx.Length - 1) As Double
                For m = 0 To by.Length - 1 Step 4
                    Dim k1 As Single = BitConverter.ToSingle(by, m)
                    yy(ik) = k1
                    ik = ik + 1
                Next
                log("收到设备PPSJ, Point=" & yy.Length, False)
                flag_UploadFreq = True
                SendFreqSub(device_freqStart, device_freqStep, xx, yy)
            Catch ex As Exception
                log("接收频谱数据-->ERR-->" & ex.ToString)
            End Try
            Sleep(800)
        End While
    End Sub

    Private Sub SendFreqSub(ByVal freqStart As Double, ByVal freqStep As Double, ByVal xx() As Double, ByVal yy() As Double)
        If TD.isConnected = False Then
            log("由于与服务器已失去连接，数据未发送……")
            End
            Return
        End If

        Dim tf As New TekFreq
        tf.freqBegin = freqStart
        tf.freqStep = freqStep
        tf.xx = xx
        tf.yy = yy
        SendFreq(freqStart, freqStep, xx, yy)
        'Dim th As New Thread(AddressOf SendFreq)
        'th.Start(tf)
    End Sub
    Public Function GetScreenImage() As Bitmap
        Dim tScreenRect As New Rectangle(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
        Dim tSrcBmp As New Bitmap(tScreenRect.Width, tScreenRect.Height)
        Dim gp As Graphics = Graphics.FromImage(tSrcBmp)
        gp.CopyFromScreen(0, 0, 0, 0, tScreenRect.Size)
        gp.DrawImage(tSrcBmp, 0, 0, tScreenRect, GraphicsUnit.Pixel)
        Return tSrcBmp
    End Function
    Public Function img2data(ByVal bmp As Bitmap) As Byte()
        Try
            Dim ms As New MemoryStream
            bmp.Save(ms, Imaging.ImageFormat.Jpeg)
            Dim arr(ms.Length) As Byte
            ms.Position = 0
            ms.Read(arr, 0, ms.Length)
            ms.Close()
            Return arr
        Catch ex As Exception
            Return Nothing
        End Try
    End Function
    Public Function data2img(ByVal by() As Byte) As Bitmap
        Try

            Dim ms As New MemoryStream(by)
            Dim bitmap As New Bitmap(ms)
            Return bitmap
        Catch ex As Exception
            Return Nothing
        End Try
    End Function
    Public Const WM_LBUTTONDOWN As Integer = 513 ' 鼠标左键按下 
    Public Const WM_LBUTTONUP As Integer = 514 ' 鼠标左键抬起 
    Public Const WM_RBUTTONDOWN As Integer = 516 ' 鼠标右键按下 
    Public Const WM_RBUTTONUP As Integer = 517 ' 鼠标右键抬起 
    Public Const WM_MBUTTONDOWN As Integer = 519 ' 鼠标中键按下 
    Public Const WM_MBUTTONUP As Integer = 520 ' 鼠标中键抬起 
    Public Const MOUSEEVENTF_MOVE As Integer = &H1 ' 移动鼠标   
    Public Const MOUSEEVENTF_LEFTDOWN As Integer = &H2 ' 鼠标左键按下  
    Public Const MOUSEEVENTF_LEFTUP As Integer = &H4 ' 鼠标左键抬起  
    Public Const MOUSEEVENTF_RIGHTDOWN As Integer = &H8 ' 鼠标右键按下  
    Public Const MOUSEEVENTF_RIGHTUP As Integer = &H10 ' 鼠标右键抬起   
    Public Const MOUSEEVENTF_MIDDLEDOWN As Integer = &H20 ' 鼠标中键按下 
    Public Const MOUSEEVENTF_MIDDLEUP As Integer = &H40 ' 鼠标中键抬起   
    Public Const MOUSEEVENTF_ABSOLUTE As Integer = &H8000 ' 绝对坐标
    Private Sub click_left(ByVal x As Integer, ByVal y As Integer)
      
        SetCursorPos(x + 3, y + 3)
        mouse_event(&H2, 0, 0, 0, 0)
        mouse_event(&H4, 0, 0, 0, 0)

    End Sub
    Private Sub click_right(ByVal x As Integer, ByVal y As Integer)

        SetCursorPos(x + 3, y + 3)
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0)
        mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0)

    End Sub
    Private Sub ReStartWindows()
        Process.Start("shutdown.exe", "-r -t 0")
    End Sub
    Private Sub ShutdownWindows()
        Process.Start("shutdown.exe", "-s -t 0")
    End Sub
#Region "操作设备"

    Public Function spec_WaitForTraceReady() As Boolean
        Dim r As Boolean = False
        Dim i As IntPtr = SPECTRUM_WaitForTraceReady(1000, r)
        If i = 101 Then
            flag_isConnected = False
            ini()
        End If
        If i = 0 Then
            Return r
        End If
        Return False
    End Function
    Public Function Spec_AcquireTrace()
        Dim i As IntPtr = SPECTRUM_AcquireTrace()
        If i = 101 Then
            flag_isConnected = False
            ini()
        End If
        If i = 0 Then
            Return True
        End If
        Return False
    End Function
    Public Function Spec_SetEnable(ByVal enable As Boolean) As Boolean
        Dim i As IntPtr = SPECTRUM_SetEnable(enable)
        If i = 101 Then
            flag_isConnected = False
            ini()
        End If
        If i = 0 Then
            Return True
        End If
        Return False
    End Function
    Public Function SetCenterFreq(ByVal freqCenter As Double) As Boolean
        Dim i As IntPtr = CONFIG_SetCenterFreq(freqCenter)

        If i = 101 Then
            flag_isConnected = False
            ini()
        End If
        If i = 0 Then
            Return True
        End If
        Return False
    End Function
    Public Function GetFreqPara() As Spectrum_Settings
        Dim size As Integer = Marshal.SizeOf(GetType(Spectrum_Settings))
        Dim pt As IntPtr = Marshal.AllocHGlobal(size)
        Dim i As IntPtr = SPECTRUM_GetSettings(pt)
        If i = 101 Then
            flag_isConnected = False
            ini()
        End If
        If i = 0 Then
            Dim SSettings As Spectrum_Settings = Marshal.PtrToStructure(pt, GetType(Spectrum_Settings))

            Return SSettings
        End If
        Return Nothing
    End Function
    Public Function SetFreqPara(ByVal s As Spectrum_Settings) As Boolean
        Dim i As IntPtr = SPECTRUM_SetSettings(s)
        If i = 101 Then
            flag_isConnected = False
            ini()
        End If
        If i = 0 Then
            Return True
        End If
        Return False
    End Function
    Public Function SearchDevice() As String
        Dim numDevicesFound As IntPtr
        Dim deviceIDs(10) As IntPtr
        Dim deviceSerial(10) As IntPtr
        Dim deviceType(10) As IntPtr

        Dim i As IntPtr = DEVICE_SearchInt(numDevicesFound, deviceIDs, deviceSerial, deviceType)
        If numDevicesFound = 0 Then Return ""
        log("numDevicesFound=" & numDevicesFound.ToInt32)
        log("deviceIDs=" & deviceIDs(0).ToString)
        log("deviceSerial=" & Marshal.PtrToStringAnsi(deviceSerial(0).ToString))
        log("deviceType=" & Marshal.PtrToStringAnsi(deviceType(0).ToString))

        'If numDevicesFound = 0 Then Return ""
        If deviceIDs.Count > 0 Then
            Return deviceIDs(0)
        Else
            Return ""
        End If
    End Function
    Public Function Connect2Device(ByVal id As String) As Boolean
        log("连接设备")
        If id = "" Then Return False
        Dim i As IntPtr = DEVICE_Connect(id)
        If i = 0 Then
            log("已连接")
            Return True
        Else
            log("连接失败")
            Return False
        End If
    End Function
    Public Function DeviceReset(ByVal id As String) As Boolean
        log("重置设备")
        If id = "" Then Return False
        Dim i As IntPtr = DEVICE_Reset(id)
        If i = 0 Then
            log("已重置")
            Return True
        Else
            log("重置失败")
            Return False
        End If
    End Function

    Public Function DeviceRun() As Boolean
        Dim i As IntPtr = DEVICE_Run()
        log(Code2Msg(i))
        If i = 101 Then
            flag_isConnected = False
            ini()
        End If
        If i = 0 Then
            Return True
        End If
        Return False
    End Function
    Public Function DeviceStop() As Boolean
        If IsNothing(thread_ReviceFreq) = False Then
            Try
                thread_ReviceFreq.Abort()
            Catch ex As Exception

            End Try
        End If
        If IsNothing(thread_ReciveAudio) = False Then
            Try
                thread_ReciveAudio.Abort()
            Catch ex As Exception

            End Try
        End If
        flag_getData = False
        Dim i As IntPtr = DEVICE_Stop()
        If i = 101 Then
            flag_isConnected = False
            ini()
        End If
        If i = 0 Then
            Return True
        End If
        Return False
    End Function

#End Region
End Module
