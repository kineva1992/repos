' =============================================================
' BytesRoad.NetSuit : A free network library for .NET platform 
' =============================================================
'
' Copyright (C) 2004-2005 BytesRoad Software
' 
' Project Info: http://www.bytesroad.com/NetSuit/
' 
' This program is free software; you can redistribute it and/or
' modify it under the terms of the GNU General Public License
' as published by the Free Software Foundation; either version 2
' of the License, or (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
'
' You should have received a copy of the GNU General Public License
' along with this program; if not, write to the Free Software
' Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
' ========================================================================== 
'  FTP Sample
'
'  File:		FtpDownloadFile.vb
'  Summary:	    Demonstrates how to download file from the FTP 
'				server by using BytesRoad.NetSuit Library.
' ========================================================================== 

Imports System
Imports System.Diagnostics
Imports System.IO

Imports BytesRoad.Diag
Imports BytesRoad.Net.Ftp

Module Module1
    Sub ShowGreeting()
        Console.WriteLine("(C) Copyright 2004-2005 BytesRoad Software. All rights reserved.")
        Console.WriteLine("-----------------------------------------------------------")
        Console.WriteLine("Description:")
        Console.WriteLine("     DownloadFile sample download specified file from the")
        Console.WriteLine("     FTP server.")
        Console.WriteLine("-----------------------------------------------------------")
    End Sub

    Sub Main()
        Dim ftp As FtpClient = Nothing

        ShowGreeting()

        ' setup tracing
        Try
            SetupTraceListener()
        Catch e As Exception
            Console.WriteLine("Warning: Unable to setup tracing ({0}).", e.Message)
        End Try

        ' set timeout to 10 seconds
        Dim timeout As Integer = 10000
        Try
            ' ask for the server name, ftp.microsoft.com is default
            Console.Write("Server name (press ENTER for 'ftp.microsoft.com'): ")
            Dim server As String = Console.ReadLine
            If ((server Is Nothing) OrElse (server.Length = 0)) Then
                server = "ftp.microsoft.com"
            End If

            ' ask for the port, 21 is default
            Dim port As Integer = 21
            Console.Write("Server port (press ENTER for 21): ")
            Dim sPort As String = Console.ReadLine
            If ((Not sPort Is Nothing) AndAlso (sPort.Length <> 0)) Then
                port = Convert.ToInt32(sPort, 10)
            End If

            ' ask for the source file path
            Console.Write("Source file path (press ENTER for '/bussys/readme.txt'): ")
            Dim srcFilePath As String = Console.ReadLine
            If ((srcFilePath Is Nothing) OrElse (srcFilePath.Length = 0)) Then
                srcFilePath = "bussys/readme.txt"
            End If

            Dim dstFilePath As String = DefDestFilePath(srcFilePath)

            ' ask for the destination file path
            Console.Write("Destination file path (press ENTER for '{0}'): ", dstFilePath)
            Dim path As String = Console.ReadLine
            If ((Not path Is Nothing) AndAlso (path.Length <> 0)) Then
                dstFilePath = path
            End If

            ' create an instance
            ftp = New FtpClient()

            ' connect to the ftp server
            Dim res As FtpResponse = ftp.Connect(timeout, server, port)
            Console.WriteLine(res.RawString & Environment.NewLine)

            ' login 
            ftp.Login(timeout, "anonymous", "a@a.com")

            ' download the file
            ftp.GetFile(timeout, dstFilePath, srcFilePath)

            Console.WriteLine("Downloading is completed.")

            ' disconnect from the FTP server
            ftp.Disconnect(timeout)
        Catch e As FtpErrorException
            Console.WriteLine("ERROR: {0} ({1}).", e.Message, e.Response.RawString)
            If (ftp.IsConnected) Then
                ftp.Disconnect(timeout)
            End If
        Catch e As Exception
            Console.WriteLine("FATAL ERROR: {0}", e.Message)
        End Try
        If (Not ftp Is Nothing) Then
            ftp.Dispose()
        End If
        Console.WriteLine("Press ENTER to exit...")
        Console.ReadLine()
    End Sub


    ' Setup tracing options - redirect all tracing
    ' into the 'FtpTrace_dVB.txt' file located
    ' in the same directory as DownloadFileVB.exe
    Private Sub SetupTraceListener()
        Dim traceFileName As String = ConstructFileName("FtpTrace_dVB.txt")
        NSTraceOptions.Level = TraceLevel.Error
        Dim listener1 As New TextWriterTraceListener(traceFileName)
        NSTraceOptions.Listeners.Add(listener1)
        NSTraceOptions.AutoFlush = True
    End Sub

    Private Function ConstructFileName(ByVal fileName As String) As String
        Dim ret As String = ".\" & fileName
        Try
            Dim oLocal As Process = Process.GetCurrentProcess
            Dim oModule As ProcessModule = oLocal.MainModule
            Dim curDir As String = Path.GetDirectoryName(oModule.FileName)
            If ((curDir.EndsWith("\") <> True) And (curDir.EndsWith("/") <> True)) Then
                curDir = (curDir & "\")
            End If
            ret = (curDir & fileName)
        Catch e As Exception
            Console.WriteLine("Warning: Can't construct the file path: {0}", e.ToString)
        End Try
        Return ret
    End Function

    Private Function DefDestFilePath(ByVal srcFilePath As String) As String
        Dim fileName As String = Nothing
        Dim idx As Integer = srcFilePath.LastIndexOfAny(New Char() {"\", "/"})
        If (idx = -1) Then
            fileName = srcFilePath
        Else
            fileName = srcFilePath.Substring(idx + 1)
        End If
        Return ConstructFileName(fileName)
    End Function
End Module
