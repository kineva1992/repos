@echo off

echo =================================================================
echo * BytesRoad.NetSuit : a free network library for .NET platform. *
echo * ============================================================= *
echo *                                                               *
echo * Copyright (C) 2004-2005 BytesRoad Software                    *
echo.*                                                               *
echo * Project Info: http://www.bytesroad.com/NetSuit                *
echo =================================================================
echo.
echo Building library ...
echo.

IF EXIST .\Bin\ GOTO HaveBin
mkdir Bin
:HaveBin


cd .\Src\BytesRoad.Diag\
call build_diag.bat
cd ..\..\

cd .\Src\BytesRoad.Net.Sockets\
call build_sockets.bat
cd ..\..\

cd .\Src\BytesRoad.Net.Ftp\
call build_ftp.bat
cd ..\..\

echo.
echo Building is done
pause
