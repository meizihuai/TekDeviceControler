Imports System
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports System.Threading.Thread
Imports System.IO
Module Module_DLL
    Public Function Code2Msg(ByVal code As IntPtr) As String
        Dim int As IntPtr = GetErrorString(code)
        Dim result As String = Marshal.PtrToStringAnsi(int)
        Return result
    End Function
    <DllImport("RSA_API.dll", entrypoint:="DEVICE_Run", CharSet:=CharSet.Ansi, exactspelling:=False, CallingConvention:=CallingConvention.Cdecl)>
    Public Function DEVICE_Run() As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="DEVICE_Stop", CharSet:=CharSet.Ansi, exactspelling:=False, CallingConvention:=CallingConvention.Cdecl)>
    Public Function DEVICE_Stop() As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="DEVICE_Reset", CharSet:=CharSet.Ansi, exactspelling:=False, CallingConvention:=CallingConvention.Cdecl)>
    Public Function DEVICE_Reset(ByVal deviceID As Int32) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="AUDIO_SetFrequencyOffset", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function AUDIO_SetFrequencyOffset(ByVal freqOffsetHz As Double) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="AUDIO_SetMute", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function AUDIO_SetMute(ByVal mute As Boolean) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="AUDIO_SetMode", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function AUDIO_SetMode(ByVal mode As AudioDemodMode) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="AUDIO_SetVolume", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function AUDIO_SetVolume(ByVal f As Single) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="AUDIO_GetEnable", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function AUDIO_GetEnable(ByRef enable As Boolean) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="AUDIO_Start", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function AUDIO_Start() As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="AUDIO_Stop", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function AUDIO_Stop() As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="AUDIO_GetData", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function AUDIO_GetData(ByVal data As IntPtr, ByVal inSize As UInt16, ByRef outSize As UInt16) As IntPtr

    End Function
    Public Enum AudioDemodMode

        '''ADM_FM_8KHZ -> 0
        ADM_FM_8KHZ = 0

        '''ADM_FM_13KHZ -> 1
        ADM_FM_13KHZ = 1

        '''ADM_FM_75KHZ -> 2
        ADM_FM_75KHZ = 2

        '''ADM_FM_200KHZ -> 3
        ADM_FM_200KHZ = 3

        '''ADM_AM_8KHZ -> 4
        ADM_AM_8KHZ = 4

        ADM_NONE
    End Enum
    Dim swLock As Object
    Public Sub log(ByVal str As String)
        str = Now.ToString("[HH:mm:ss] ") & str
        Console.WriteLine(str)
        Return
        If Directory.Exists("logs") = False Then
            Directory.CreateDirectory("logs")
        End If
        If IsNothing(swLock) Then swLock = New Object
        SyncLock swLock
            Dim path As String = "logs\" & Now.ToString("yyyy_MM_dd") & ".txt"
            Dim sw As New StreamWriter(path, True, Encoding.Default)
            sw.WriteLine(str)
            sw.Close()
        End SyncLock      
    End Sub
    Public Sub log(ByVal str As String, ByVal flagRecord As Boolean)
        str = Now.ToString("[HH:mm:ss] ") & str
        Console.WriteLine(str)
        If flagRecord Then
            If Directory.Exists("logs") = False Then
                Directory.CreateDirectory("logs")
            End If
            If IsNothing(swLock) Then swLock = New Object
            SyncLock swLock
                Dim path As String = "logs\" & Now.ToString("yyyy_MM_dd") & ".txt"
                Dim sw As New StreamWriter(path, True, Encoding.Default)
                sw.WriteLine(str)
                sw.Close()
            End SyncLock
        End If
       
    End Sub
    <DllImport("RSA_API.dll", EntryPoint:="DEVICE_Connect", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function DEVICE_Connect(ByVal deviceID As Int32) As IntPtr

    End Function
    <DllImport("RSA_API.dll", EntryPoint:="DEVICE_Disconnect", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function DEVICE_Disconnect(ByVal deviceID As Int32) As IntPtr

    End Function
    <DllImport("RSA_API.dll", EntryPoint:="DEVICE_GetEnable", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function DEVICE_GetEnable(ByRef enable As Boolean) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="CONFIG_SetCenterFreq", CharSet:=CharSet.Ansi, exactspelling:=False, CallingConvention:=CallingConvention.Cdecl)>
    Public Function CONFIG_SetCenterFreq(ByVal refLevel As Double) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="SPECTRUM_SetSettings", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function SPECTRUM_SetSettings(ByVal setting As Spectrum_Settings) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="SPECTRUM_GetSettings", CharSet:=CharSet.Ansi, exactspelling:=False, CallingConvention:=CallingConvention.Cdecl)>
    Public Function SPECTRUM_GetSettings(ByVal Setting As IntPtr) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="SPECTRUM_GetEnable", CharSet:=CharSet.Ansi, exactspelling:=False, CallingConvention:=CallingConvention.Cdecl)>
    Public Function SPECTRUM_GetEnable(ByRef enable As Boolean) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="SPECTRUM_SetDefault", CharSet:=CharSet.Ansi, exactspelling:=False, CallingConvention:=CallingConvention.Cdecl)>
    Public Function SPECTRUM_SetDefault() As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="SPECTRUM_SetEnable", CharSet:=CharSet.Ansi, exactspelling:=False, CallingConvention:=CallingConvention.Cdecl)>
    Public Function SPECTRUM_SetEnable(ByVal enable As Boolean) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="SPECTRUM_AcquireTrace", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function SPECTRUM_AcquireTrace() As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="SPECTRUM_WaitForTraceReady", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function SPECTRUM_WaitForTraceReady(ByVal timeoutMsec As Integer, ByRef ready As Boolean) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="SPECTRUM_GetTrace", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function SPECTRUM_GetTrace(ByVal trace As Int32, ByVal maxTracePoints As Integer, ByVal traceData As IntPtr, ByRef outTracePoints As Integer) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="DEVICE_PrepareForRun", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function DEVICE_PrepareForRun() As IntPtr

    End Function
    ''获取错误string
    <DllImport("RSA_API.dll", EntryPoint:="GetErrorString", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Function GetErrorString(ByVal status As IntPtr) As IntPtr

    End Function
    <DllImport("RSA_API.dll", entrypoint:="DEVICE_SearchInt", CharSet:=CharSet.Ansi, exactspelling:=False, CallingConvention:=CallingConvention.Cdecl)>
    Public Function DEVICE_SearchInt(ByRef numDevicesFound As IntPtr, ByRef deviceIDs() As IntPtr, ByRef deviceSerial() As IntPtr, ByRef deviceType() As IntPtr) As IntPtr

    End Function

    <System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)> _
    Public Structure Spectrum_Settings

        '''double
        Public span As Double

        '''double
        Public rbw As Double

        '''boolean
        <System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.I1)> _
        Public enableVBW As Boolean

        '''double
        Public vbw As Double

        '''int
        Public traceLength As Integer

        '''SpectrumWindows->Anonymous_b441ab41_6b19_41df_b2bd_7f91ce85dd19
        Public window As SpectrumWindows

        '''SpectrumVerticalUnits->Anonymous_eff7c62c_dfab_4465_897f_5899d935d934
        Public verticalUnit As SpectrumVerticalUnits

        '''double
        Public actualStartFreq As Double

        '''double
        Public actualStopFreq As Double

        '''double
        Public actualFreqStepSize As Double

        '''double
        Public actualRBW As Double

        '''double
        Public actualVBW As Double

        '''int
        Public actualNumIQSamples As Integer
    End Structure

    Public Enum SpectrumWindows

        '''SpectrumWindow_Kaiser -> 0
        SpectrumWindow_Kaiser = 0

        '''SpectrumWindow_Mil6dB -> 1
        SpectrumWindow_Mil6dB = 1

        '''SpectrumWindow_BlackmanHarris -> 2
        SpectrumWindow_BlackmanHarris = 2

        '''SpectrumWindow_Rectangle -> 3
        SpectrumWindow_Rectangle = 3

        '''SpectrumWindow_FlatTop -> 4
        SpectrumWindow_FlatTop = 4

        '''SpectrumWindow_Hann -> 5
        SpectrumWindow_Hann = 5
    End Enum

    Public Enum SpectrumVerticalUnits

        '''SpectrumVerticalUnit_dBm -> 0
        SpectrumVerticalUnit_dBm = 0

        '''SpectrumVerticalUnit_Watt -> 1
        SpectrumVerticalUnit_Watt = 1

        '''SpectrumVerticalUnit_Volt -> 2
        SpectrumVerticalUnit_Volt = 2

        '''SpectrumVerticalUnit_Amp -> 3
        SpectrumVerticalUnit_Amp = 3

        '''SpectrumVerticalUnit_dBmV -> 4
        SpectrumVerticalUnit_dBmV = 4
    End Enum
End Module
