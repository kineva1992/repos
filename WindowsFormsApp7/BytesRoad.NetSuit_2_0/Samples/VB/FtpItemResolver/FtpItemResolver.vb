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
'  File:		FtpItemResolver.vb
'  Summary:	    Demonstrates how to customize default resolving mechanism
'				used in BytesRoad.NetSuit Library. This mechanism works
'				during reading content of the directory from the FTP server.
' ========================================================================== 

Imports System
Imports System.Diagnostics
Imports System.IO

Imports BytesRoad.Diag
Imports BytesRoad.Net.Ftp

Module Module1

    ' MyResolver class implements user 
    ' defined ftp item resolver.
    Public Class MyResolver
        Implements IFtpItemResolver

        Public Sub New()
        End Sub

        Public Function Resolve(ByVal rawString As String) As FtpItem _
        Implements IFtpItemResolver.Resolve

            Console.WriteLine("RawString: {0}", rawString)

            ' simply use the default implementation
            Return FtpClient.DefaultFtpItemResolver.Resolve(rawString)
        End Function
    End Class

    Sub ShowGreeting()
        Console.WriteLine("(C) Copyright 2004-2005 BytesRoad Software. All rights reserved.")
        Console.WriteLine("-----------------------------------------------------------")
        Console.WriteLine("Description:")
        Console.WriteLine("     FtpItemResolver sample read the content of the default")
        Console.WriteLine("     directory at the ftp.microsoft.com FTP server. ")
        Console.WriteLine("     Demonstrates how to customize default mechanism used")
        Console.WriteLine("     for ftp item resolving.")
        Console.WriteLine("-----------------------------------------------------------")
    End Sub


    Sub Main()

        ShowGreeting()

        ' setup tracing
        Try
            SetupTraceListener()
        Catch e As Exception
            Console.WriteLine("Warning: Unable to setup tracing ({0}).", e.Message)
        End Try

        Dim ftp As FtpClient = Nothing

        ' set timeout to 10 seconds
        Dim timeout As Integer = 10000
        Try
            Dim server As String = "ftp.microsoft.com"
            ftp = New FtpClient()

            ' customize the resolver
            ftp.FtpItemResolver = New MyResolver()

            ' connect to the ftp server
            Console.Write("Connecting to '{0}'... ", server)
            Dim res As FtpResponse = ftp.Connect(timeout, server, 21)
            Console.WriteLine("done")
            Console.WriteLine(res.RawString & Environment.NewLine)

            ' login 
            ftp.Login(timeout, "anonymous", "a@a.com")

            ' switch between MS-DOS and Unix directory
            ' listing styles
            Console.WriteLine("Send 'SITE DIRSTYLE'")
            res = ftp.SendCommand(timeout, "SITE DIRSTYLE")
            Console.WriteLine(res.RawString & Environment.NewLine)

            ' get default directory content
            Dim items As FtpItem() = ftp.GetDirectoryList(timeout, Nothing)
            Console.WriteLine("{0}Default directory for '{1}': {2}", _
                        Environment.NewLine, server, Environment.NewLine)
            Console.WriteLine("    Type            Name                          Size     Time")
            Console.WriteLine("------------------------------------------------------------------------------")

            Dim idx As Integer = 0
            Do While (idx < items.Length)
                Dim item As FtpItem = items(idx)
                If (item.ItemType <> FtpItemType.Unresolved) Then
                    Console.WriteLine("  {0,-10} {1,-25}     {2,10}     {3}", _
                            item.ItemType.ToString, _
                            item.Name, _
                            item.Size, _
                            item.Date.ToString)
                Else
                    Console.WriteLine(item.RawString)
                End If
                idx += 1
            Loop
            ftp.Disconnect(timeout)

        Catch e As FtpErrorException
            ' non fatal error occurs...
            Console.WriteLine("ERROR: {0} ({1}).", e.Message, e.Response.RawString)

            ' let's close the connection gracefully
            If (ftp.IsConnected) Then
                ftp.Disconnect(timeout)
            End If

        Catch e As Exception
            Console.WriteLine("FATAL ERROR: {0}", e.Message)
        End Try

        ' release all resources
        If (Not ftp Is Nothing) Then
            ftp.Dispose()
        End If

        Console.WriteLine("")
        Console.WriteLine("Press ENTER to exit...")
        Console.ReadLine()
    End Sub

    ' Setup tracing options - redirect all tracing
    ' into the 'FtpTrace_rVB.txt' file located
    ' in the same directory as FtpItemResolverVB.exe
    Private Sub SetupTraceListener()
        Dim traceFileName As String = ConstructFileName("FtpTrace_rVB.txt")
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
        Catch exception1 As Exception
            Console.WriteLine("Warning: Can't construct the file path: {0}", exception1.ToString)
        End Try
        Return ret
    End Function

End Module
