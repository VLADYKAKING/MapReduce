@echo off

rem MASTER
echo Starting Master...
start "Master Server" ".\Master\bin\Debug\net9.0\Master.exe" 50010
timeout /t 1 /nobreak > nul


rem MAP WORKERS
echo Starting Worker...
start "Worker Server" ".\Worker\bin\Debug\net9.0\Worker.exe" 50021 50010 Map
timeout /t 1 /nobreak > nul

echo Starting Worker...
start "Worker Server" ".\Worker\bin\Debug\net9.0\Worker.exe" 50022 50010 Map
timeout /t 1 /nobreak > nul

echo Starting Worker...
start "Worker Server" ".\Worker\bin\Debug\net9.0\Worker.exe" 50023 50010 Map
timeout /t 1 /nobreak > nul

echo Starting Worker...
start "Worker Server" ".\Worker\bin\Debug\net9.0\Worker.exe" 50024 50010 Map
timeout /t 1 /nobreak > nul


rem REDUCE WORKERS
echo Starting Worker...
start "Worker Server" ".\Worker\bin\Debug\net9.0\Worker.exe" 50031 50010 Reduce
timeout /t 1 /nobreak > nul

echo Starting Worker...
start "Worker Server" ".\Worker\bin\Debug\net9.0\Worker.exe" 50032 50010 Reduce
timeout /t 1 /nobreak > nul

echo Starting Worker...
start "Worker Server" ".\Worker\bin\Debug\net9.0\Worker.exe" 50033 50010 Reduce
timeout /t 1 /nobreak > nul

echo Starting Worker...
start "Worker Server" ".\Worker\bin\Debug\net9.0\Worker.exe" 50034 50010 Reduce
timeout /t 1 /nobreak > nul


rem CLIENT
echo Starting Client...
start "Worker Server" ".\Client\bin\Debug\net9.0\Client.exe" 50010 C:\\Users\\V\\Desktop\\MapReduce\\sample.txt
timeout /t 1 /nobreak > nul