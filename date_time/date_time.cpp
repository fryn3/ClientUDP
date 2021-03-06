// date_time.cpp : Defines the entry point for the console application.
// Программа для чтения журнала отказа

#include "stdafx.h"
using namespace std;
int main() {
    struct tm newtime;
    __time32_t aclock;
    _time32(&aclock);   // Get time in seconds.  
    _localtime32_s(&newtime, &aclock);   // Convert time to struct tm form.  
    ofstream fout;
    fout.open("code\\version.h"); // связываем объект с файлом
                                  //fout.open("cppstudio.txt");
    if (!fout.is_open()) // если файл не открыт
    {
        cout << "Can't open file !" << endl; // сообщить об этом
        cin.get();
        return 1;
    } else {
        ostringstream  ss;
        ss.fill('0');   // ругается на это в х64
        ss << setw(2) << newtime.tm_hour << ":" << setw(2) << newtime.tm_min << ":" << setw(2) << newtime.tm_sec << " ";
        ss << setw(2) << newtime.tm_mday << "/" << setw(2) << newtime.tm_mon + 1 << "/" << newtime.tm_year + 1900;
        string s = ss.str();
        fout << "#ifndef __VERSION_H__  // The file is created automatically by the program date_time.exe" << endl;
        fout << "  #define __VERSION_H__" << endl;
        fout << "  #define VERSION_TIME      (\"";
        fout << s;
        fout << "\")" << endl;
        fout << "  #define LEN_VERSION_TIME  (";
        fout << s.length();
        fout << ")" << endl;
        fout << "#endif    //  __VERSION_H__" << endl;
        fout.close(); // закрываем файл
    }
    return 0;
}
