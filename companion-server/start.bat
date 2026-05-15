@echo off

echo Installing dependencies...
pip install -r requirements.txt --user

echo Starting Virtual Buddy companion server...
python app.py

pause