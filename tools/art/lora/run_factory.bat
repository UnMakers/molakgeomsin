@echo off
set PYTHONIOENCODING=utf-8
set PYTHONUTF8=1
cd /d C:\Users\sjf1t\RuinedSword\Build\lora
C:\Users\sjf1t\wag-train\venv\Scripts\python.exe -u lora_factory.py %* >> C:\Users\sjf1t\RuinedSword\Build\lora\factory_console.txt 2>&1
