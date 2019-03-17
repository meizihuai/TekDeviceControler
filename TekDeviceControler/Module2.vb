Imports System
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports System.Threading.Thread
Imports System.IO
Imports Newtonsoft
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Net
Module Module2
    Public Function getValueByCanShuQu(ByVal str As String, ByVal quarName As String) As String
        If InStr(str, ";") = False Then Return ""
        Dim st() As String = str.Split(";")
        For Each sh In st
            If InStr(sh, "=") Then
                Dim sk() As String = sh.Split("=")
                If sk.Length <> 2 Then Return ""
                If sk(0) = quarName Then
                    Return sk(1)
                End If
            End If
        Next
    End Function
    Public Sub CheckLocationDllFile(ByVal fileName As String)
        Dim path As String = fileName

        If File.Exists(path) = False Then
            Console.WriteLine("正在下载必要组件……" & fileName)
            Dim url As String = "http://123.207.31.37:8082/update/TekDeviceControler/" & fileName & "?" & Now.Ticks
            Download(url, path)
        End If
    End Sub
    Private Sub Download(ByVal url As String, ByVal path As String)
        If File.Exists(path) Then File.Delete(path)
        While True
            Try
                Dim req As HttpWebRequest = WebRequest.Create(url)
                req.Accept = "*/*"
                req.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; zh-CN; rv:1.9.2.13) Gecko/20101203 Firefox/3.6.13"
                req.KeepAlive = True
                req.ContentType = "application/x-www-form-urlencoded"
                req.Method = "GET"
                Dim rp As HttpWebResponse = req.GetResponse
                Dim sum As Integer = 0
                Dim buffer() As Byte
                While True
                    Dim by(102400000) As Byte
                    Dim num As Integer = rp.GetResponseStream.Read(by, 0, by.Count)
                    If num = 0 Then
                        Exit While
                    Else
                        sum = sum + num
                        If IsNothing(buffer) Then
                            ReDim buffer(num - 1)
                            Array.Copy(by, 0, buffer, 0, num)
                        Else
                            Dim bu(num - 1) As Byte
                            Array.Copy(by, 0, bu, 0, num)
                            buffer = buffer.Concat(bu).ToArray
                        End If
                    End If
                End While

                Dim stream As New FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite)
                Dim bw As New BinaryWriter(stream)
                bw.Write(buffer)
                bw.Close()

                Exit While
            Catch ex As Exception

            End Try
        End While
    End Sub
    Public Function GetNorResult(ByVal paraName As String, ByVal result As String) As String
        If InStr(result, ";") = False Then Return ""
        For Each itm In result.Split(";")
            If InStr(itm, "=") Then
                Dim k As String = itm.Split("=")(0)
                Dim v As String = itm.Split("=")(1)
                If k = paraName Then
                    Return v
                End If
            End If
        Next
        Return ""
    End Function
    Public Function GetH(ByVal uri As String, ByVal msg As String) As String
        Dim num As Integer = 0
        While True
            Try
                Dim req As HttpWebRequest = WebRequest.Create(uri & msg)
                req.Accept = "*/*"
                req.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; zh-CN; rv:1.9.2.13) Gecko/20101203 Firefox/3.6.13"
                req.CookieContainer = New CookieContainer
                req.KeepAlive = True
                req.ContentType = "application/x-www-form-urlencoded"
                req.Method = "GET"
                Dim b() As Byte = Encoding.Default.GetBytes(msg)
                Dim rp As HttpWebResponse = req.GetResponse
                Dim str As String = New StreamReader(rp.GetResponseStream(), Encoding.UTF8).ReadToEnd
                b = Encoding.Default.GetBytes(str)
                Return str
            Catch ex As Exception

            End Try
            num = num + 1
            If num = 4 Then Return ""
        End While
    End Function
    Public Sub HandleUpdate()
        Try
            Dim sw As New StreamWriter("updateinfo.ini", False, Encoding.Default)
            sw.Write(exeName)
            sw.Close()
            Dim updateurl As String = "http://123.207.31.37:8080/?func=autoUpdate&updateFunc=getupdate&exename=" & exeName & "&version=" & myVerison
            Dim tmp As String = GetH(updateurl, "")
            Dim result As String = GetNorResult("result", tmp)
            Dim msg As String = GetNorResult("msg", tmp)
            If result = "success" Then
                If File.Exists("update.exe") Then
                    Shell("update.exe", AppWinStyle.NormalFocus)
                    End
                End If
            Else
                log("没有可用更新")
            End If
        Catch ex As Exception

        End Try
    End Sub
End Module
