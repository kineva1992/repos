@echo off
echo Building demo for BytesRoad.NetSuit Library ...

if exist .\CS\Bin goto Have_cs_bin
mkdir .\CS\Bin\
:Have_cs_bin

if exist .\VB\Bin goto Have_vb_bin
mkdir .\VB\Bin\
:Have_VB_bin

if exist .\Bin goto Have_complex_bin
mkdir .\Bin
:Have_complex_bin



if EXIST .\Bin\BytesRoad.Diag.dll goto hg_good_diag
copy ..\Bin\BytesRoad.Diag.dll .\Bin\BytesRoad.Diag.dll
:hg_good_diag

if EXIST .\Bin\BytesRoad.Net.Sockets.dll goto hg_good_sockets
copy ..\Bin\BytesRoad.Net.Sockets.dll .\Bin\BytesRoad.Net.Sockets.dll
:hg_good_sockets

if EXIST .\Bin\BytesRoad.Net.Ftp.dll goto hg_good_ftp
copy ..\Bin\BytesRoad.Net.Ftp.dll .\Bin\BytesRoad.Net.Ftp.dll
:hg_good_ftp


if EXIST .\CS\Bin\BytesRoad.Diag.dll goto cs_good_diag
copy ..\Bin\BytesRoad.Diag.dll .\CS\Bin\BytesRoad.Diag.dll
:cs_good_diag

if EXIST .\CS\Bin\BytesRoad.Net.Sockets.dll goto cs_good_sockets
copy ..\Bin\BytesRoad.Net.Sockets.dll .\CS\Bin\BytesRoad.Net.Sockets.dll
:cs_good_sockets

if EXIST .\CS\Bin\BytesRoad.Net.Ftp.dll goto cs_good_ftp
copy ..\Bin\BytesRoad.Net.Ftp.dll .\CS\Bin\BytesRoad.Net.Ftp.dll
:cs_good_ftp

if EXIST .\VB\Bin\BytesRoad.Diag.dll goto vb_good_diag
copy ..\Bin\BytesRoad.Diag.dll .\VB\Bin\BytesRoad.Diag.dll
:vb_good_diag

if EXIST .\VB\Bin\BytesRoad.Net.Sockets.dll goto vb_good_sockets
copy ..\Bin\BytesRoad.Net.Sockets.dll .\VB\Bin\BytesRoad.Net.Sockets.dll
:vb_good_sockets

if EXIST .\VB\Bin\BytesRoad.Net.Ftp.dll goto vb_good_ftp
copy ..\Bin\BytesRoad.Net.Ftp.dll .\VB\Bin\BytesRoad.Net.Ftp.dll
:vb_good_ftp

cd .\cs\FtpDirectoryList\
call build.bat
cd ..\..\

cd .\cs\FtpDownloadFile\
call build.bat
cd ..\..\

cd .\cs\FtpItemResolver\
call build.bat
cd ..\..\

cd .\cs\FtpUploadFile\
call build.bat
cd ..\..\

cd .\SocketsHttpGet\
call build.bat
cd ..

cd .\vb\FtpDirectoryList\
call build.bat
cd ..\..\

cd .\vb\FtpDownloadFile\
call build.bat
cd ..\..\

cd .\vb\FtpItemResolver\
call build.bat
cd ..\..\

cd .\vb\FtpUploadFile\
call build.bat
cd ..\..\
