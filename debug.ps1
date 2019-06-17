& .\build.ps1
& mono --debug --debugger-agent=transport=dt_socket,server=y,address=127.0.0.1:50000 bin/main.exe
