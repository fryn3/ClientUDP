#include "stdafx.h"
#include <iostream>
#include <fstream>
#include <ctime>
#include <iomanip>      // std::setfill, std::setw
#include <sstream>
using namespace std;
int main()
{
	struct tm newtime;
	__time32_t aclock;
	_time32(&aclock);   // Get time in seconds.  
	_localtime32_s(&newtime, &aclock);   // Convert time to struct tm form.  
	ofstream fout;
	fout.open("code\\version.h"); // связываем объект с файлом
	if (!fout.is_open()) // если файл не открыт
	{
		cout << "Can't open file !\n"; // сообщить об этом
		cin.get();
		return 1;
	}
	else
	{
		ostringstream  ss;
		ss << newtime.tm_year + 1900 << "/";
		ss << setfill('0') << setw(2) << newtime.tm_mon + 1;
		ss << "/";
		ss << setw(2) << newtime.tm_mday;
		ss << " ";
		ss << setw(2) << newtime.tm_hour;
		ss << ":";
		ss << setw(2) << newtime.tm_min;
		ss << ":";
		ss << setw(2) << newtime.tm_sec;
		string s = ss.str();
		fout << "#ifndef __VERSION_H__" << endl;
		fout << "	#define __VERSION_H__" << endl;
		fout << "	#define VERSION_TIME			(\"";
		fout << s;
		fout << "\")" << endl;
		fout << "	#define LEN_VERSION_TIME	(";
		fout << s.length();
		fout << ")" << endl;
		fout << "#endif 	//	__VERSION_H__" << endl;
		fout.close(); // закрываем файл
	}
	return 0;
}
