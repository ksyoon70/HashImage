// ImageHashMng.h : ImageHashMng DLL의 주 헤더 파일
//

#pragma once

#ifndef __AFXWIN_H__
	#error "PCH에 대해 이 파일을 포함하기 전에 'pch.h'를 포함합니다."
#endif

#include "resource.h"		// 주 기호입니다.
#include "imageHashMng_exp.h"
#include <string>
#include <vector>
#include <filesystem>    // C++17 파일 시스템 지원
#include <openssl/sha.h> // OpenSSL 라이브러리 (SHA256 사용)
#include <fstream>
#include <map>
#include <io.h>

// CImageHashMngApp
// 이 클래스 구현에 대해서는 ImageHashMng.cpp를 참조하세요.
//

class CImageHashMngApp : public CWinApp
{
public:
	CImageHashMngApp();

// 재정의입니다.
public:
	virtual BOOL InitInstance();
	BOOL InitEngine(TCHAR* strHashFilePath);
	BOOL GenHash(TCHAR* strImagePath);
	BOOL GetKey(TCHAR* strImagePath, TCHAR* key, size_t keyBufferSize);		//이미지 파일을 넣으면 key를 얻어오는 함수
	BOOL FindValue(TCHAR* key, TCHAR* value, size_t valueBufferSize);
	BOOL CloseEngine();

	DECLARE_MESSAGE_MAP()

private:
	void ProcessFolder(const CString& folderPath);
	CString GenerateFileHash(const CString& filePath);
	std::vector<CString> GetFilesInFolder(const CString& folderPath);
	CString ConvertPathToMultibyte(const std::filesystem::path& path);
	CString ExtractTextFromFileName(const CString& fileName);
	void SaveDictionaryToFile(const std::map<CString, CString>& dictionary, const CString& filePath);
	BOOL LoadDictionaryFromFile(const CString& filePath, std::map<CString, CString>& dictionary);

	std::map<CString, CString> dictionary;

};
