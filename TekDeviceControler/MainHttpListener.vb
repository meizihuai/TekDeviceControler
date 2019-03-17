Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.IO
Imports System.Math
Imports System.Net.Sockets
Imports System.Net
Imports System.Net.HttpListener
Imports System.Data
Imports System.Threading
Imports System.Threading.Thread
Imports System
Imports System.Int32
Imports System.BitConverter
Imports Newtonsoft
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Xml
Imports System.Web
Public Class MainHttpListener
    Dim MainHttpListener As HttpListener
    Dim myServerURI As String
    Sub New(ByVal url As String)
        Try
            MainHttpListener = New HttpListener
            MainHttpListener.Prefixes.Add(url)
            myServerURI = url
        Catch ex As Exception
            log("MainHttpListener初始化失败，" & ex.Message)
        End Try
    End Sub
    Public Sub Start()
        Try
            MainHttpListener.Start()
            log("MainHttpListener已开启,URL=" & myServerURI)
            MainHttpListener.BeginGetContext(New AsyncCallback(AddressOf GetContextCallBack), MainHttpListener)
            log("BroadcastWarnMsg已开启")

        Catch ex As Exception
            log("MainHttpListener开启失败" & ex.Message)
            log("请管理员权限运行CMD控制台，键入以下命令：")
            Dim str As String = "netsh http add urlacl url=" & myServerURI & "  user=Everyone"
            log(str)
            Dim fileName As String = "A_netsh.bat"
            If File.Exists(fileName) Then File.Delete(fileName)
            File.WriteAllText(fileName, str, Encoding.Default)
            log("命令已写入软件目录下 A_netsh.bat 文件中，右键管理员权限运行即可！")
        End Try

    End Sub
    Private Sub GetContextCallBack(ByVal ar As IAsyncResult)
        Try
            MainHttpListener = ar.AsyncState
            Dim context As HttpListenerContext = MainHttpListener.EndGetContext(ar)
            MainHttpListener.BeginGetContext(New AsyncCallback(AddressOf GetContextCallBack), MainHttpListener)
            HandleHttpContext(context)
        Catch ex As Exception

        End Try
    End Sub
    Private Sub HandleHttpContext(ByVal context As HttpListenerContext)
        Dim func As String = context.Request.QueryString("func")
        If func.ToLower = "test" Then
            If flagNeedRestart Then
                response(context, "false")
                Return
            Else
                response(context, "true")
                Return
            End If
           
        End If
        response(context, "false")
        Return
    End Sub
    Public Sub response(ByVal context As HttpListenerContext, ByVal msg As String)
        Try
            Dim t As New Th_ResponseStu(context, msg)
            Dim th As New Threading.Thread(AddressOf Th_Response)
            th.Start(t)
        Catch ex As Exception

        End Try
    End Sub
    Structure Th_ResponseStu
        Dim c As HttpListenerContext
        Dim m As String
        Sub New(ByVal context As HttpListenerContext, ByVal msg As String)
            c = context
            m = msg
        End Sub
    End Structure
    Private Sub Th_Response(ByVal t As Th_ResponseStu)
        If IsNothing(t) Then Return
        Try
            Dim context As HttpListenerContext = t.c
            Dim msg As String = t.m
            context.Response.StatusCode = 200
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*")
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
            context.Response.Headers.Add("Access-Control-Allow-Headers", "x-requested-with,Content-Type")
            Dim w As New StreamWriter(context.Response.OutputStream, Encoding.UTF8)
            w.Write(msg)
            w.Close()
        Catch ex As Exception

        End Try
    End Sub
End Class
