// ImageHashMng.cpp : DLL의 초기화 루틴을 정의합니다.
//

#include "pch.h"
#include "framework.h"
#include "ImageHashMng.h"


#ifdef _DEBUG
#define new DEBUG_NEW
#endif

//
//TODO: 이 DLL이 MFC DLL에 대해 동적으로 링크되어 있는 경우
//		MFC로 호출되는 이 DLL에서 내보내지는 모든 함수의
//		시작 부분에 AFX_MANAGE_STATE 매크로가
//		들어 있어야 합니다.
//
//		예:
//
//		extern "C" BOOL PASCAL EXPORT ExportedFunction()
//		{
//			AFX_MANAGE_STATE(AfxGetStaticModuleState());
//			// 일반적인 함수 본문은 여기에 옵니다.
//		}
//
//		이 매크로는 MFC로 호출하기 전에
//		각 함수에 반드시 들어 있어야 합니다.
//		즉, 매크로는 함수의 첫 번째 문이어야 하며
//		개체 변수의 생성자가 MFC DLL로
//		호출할 수 있으므로 개체 변수가 선언되기 전에
//		나와야 합니다.
//
//		자세한 내용은
//		MFC Technical Note 33 및 58을 참조하십시오.
//

// CImageHashMngApp

BEGIN_MESSAGE_MAP(CImageHashMngApp, CWinApp)
END_MESSAGE_MAP()


// CImageHashMngApp 생성

CImageHashMngApp::CImageHashMngApp()
{
	// TODO: 여기에 생성 코드를 추가합니다.
	// InitInstance에 모든 중요한 초기화 작업을 배치합니다.
}


// 유일한 CImageHashMngApp 개체입니다.

CImageHashMngApp theApp;

BOOL InitEngine(TCHAR* strHashFilePath)
{
	return theApp.InitEngine(strHashFilePath);
}

BOOL GenHash(TCHAR* strImagePath)
{
	return theApp.GenHash(strImagePath);
}

BOOL GetKey(TCHAR* strImagePath, TCHAR* key, size_t keyBufferSize)
{
	return theApp.GetKey(strImagePath, key, keyBufferSize);
}

BOOL FindValue(TCHAR* key, TCHAR* value, size_t valueBufferSize)
{
	return theApp.FindValue(key, value, valueBufferSize);
}

BOOL CloseEngine()
{
	return theApp.CloseEngine();
}




// CImageHashMngApp 초기화

BOOL CImageHashMngApp::InitInstance()
{
	CWinApp::InitInstance();

	return TRUE;
}

BOOL CImageHashMngApp::InitEngine(TCHAR* strHashFilePath)
{
	//해쉬 파일을 읽는다.
	BOOL state = FALSE;

	if (strHashFilePath == nullptr || _tcslen(strHashFilePath) == 0)
	{
		AfxMessageBox(_T("Invalid folder path."));
		return state;
	}

	CString folderPath(strHashFilePath);

	try
	{
		dictionary.clear();
		state = LoadDictionaryFromFile(folderPath, dictionary);
	}
	catch (const std::exception& ex)
	{
		CString errorMessage;
		errorMessage.Format(_T("Error: %S"), ex.what());
		AfxMessageBox(errorMessage);
		return state;
	}


	return state;
}

BOOL CImageHashMngApp::GenHash(TCHAR* strImagePath)
{
	//해당 폴더의 이미지 파일을 읽어서 해쉬 코드를 만든다.

	if (strImagePath == nullptr || _tcslen(strImagePath) == 0)
	{
		AfxMessageBox(_T("Invalid folder path."));
		return FALSE;
	}

	CString folderPath(strImagePath);

	try
	{
		ProcessFolder(folderPath);
	}
	catch (const std::exception& ex)
	{
		CString errorMessage;
		errorMessage.Format(_T("Error: %S"), ex.what());
		AfxMessageBox(errorMessage);
		return FALSE;
	}


	return TRUE;
}

BOOL CImageHashMngApp::GetKey(TCHAR* strImagePath, TCHAR* key, size_t keyBufferSize)
{
	BOOL state = FALSE;
	CString filePath = CString(strImagePath);
	CString strkey = GenerateFileHash(filePath);

	if (! strkey.IsEmpty())
	{
		_tcscpy_s(key, keyBufferSize, strkey.GetString());
		state = TRUE;
	}
	else
	{
		key = NULL;
	}

	return state;

}

BOOL CImageHashMngApp::FindValue(TCHAR* key, TCHAR* value, size_t valueBufferSize)
{
	BOOL state = FALSE;

	if (dictionary.empty())
	{
		value = NULL;
	}
	else
	{
		auto it = dictionary.find(key);
		if (it != dictionary.end())
		{
			_tcscpy_s(value, valueBufferSize, it->second.GetString());
			state = TRUE;
		}
		else
		{
			value = NULL;
		} 
	}

	return state;

}

BOOL CImageHashMngApp::CloseEngine()
{
	return TRUE;
}


void CImageHashMngApp::ProcessFolder(const CString& folderPath)
{
	// 폴더에서 이미지 파일 목록 가져오기
	auto imageFiles = GetFilesInFolder(folderPath);

	if (imageFiles.empty())
	{
		AfxMessageBox(_T("No image files found in the folder."));
		return;
	}

	// 각 이미지 파일의 해시 생성
	for (const auto& filePath : imageFiles)
	{
		CString hashCode = GenerateFileHash(filePath);
		CString message;
		CString strRecog = ExtractTextFromFileName(filePath);
		//message.Format(_T("File: %s\nHash: %s"), filePath.GetString(), hashCode.GetString());
		//AfxMessageBox(message);
		if (!strRecog.IsEmpty()) // strRecog가 비어있지 않으면 추가
		{
			dictionary[hashCode] = strRecog;
		}
	}
	if (dictionary.size() > 0)
	{
		// 딕셔너리 저장
		SaveDictionaryToFile(dictionary, _T("HashDictionary.txt"));
	}
	
}

std::vector<CString> CImageHashMngApp::GetFilesInFolder(const CString& folderPath)
{
	std::vector<CString> imageFiles;

	// 지원하는 이미지 확장자
	std::vector<CString> extensions = { _T(".jpg"), _T(".jpeg"), _T(".png"), _T(".bmp"), _T(".gif") };

	// 파일 시스템 탐색
	for (const auto& entry : std::filesystem::directory_iterator(folderPath.GetString()))
	{
		if (entry.is_regular_file())
		{
			CString filePath = ConvertPathToMultibyte(entry.path());
			CString fileExt = filePath.Mid(filePath.ReverseFind(_T('.'))).MakeLower();

			// 확장자 필터링
			if (std::find(extensions.begin(), extensions.end(), fileExt) != extensions.end())
			{
				imageFiles.push_back(filePath);
			}
		}
	}

	

	return imageFiles;
}

CString CImageHashMngApp::ConvertPathToMultibyte(const std::filesystem::path& path)
{
	// 유니코드 문자열을 가져옴
	const wchar_t* wideStr = path.c_str();

	// 변환에 필요한 버퍼 크기 계산
	int bufferSize = WideCharToMultiByte(CP_ACP, 0, wideStr, -1, nullptr, 0, nullptr, nullptr);

	if (bufferSize == 0)
		return CString(""); // 변환 실패 시 빈 문자열 반환

	// 멀티바이트 문자열로 변환
	std::vector<char> buffer(bufferSize);
	WideCharToMultiByte(CP_ACP, 0, wideStr, -1, buffer.data(), bufferSize, nullptr, nullptr);

	return CString(buffer.data());
}

CString CImageHashMngApp::ExtractTextFromFileName(const CString& fileName)
{
	// 파일 이름에서 마지막 "_"의 위치를 찾음
	int lastUnderscorePos = fileName.ReverseFind(_T('_'));
	if (lastUnderscorePos == -1)
	{
		// "_"가 없는 경우
		return _T("");
	}

	// 파일 이름에서 마지막 "."의 위치를 찾음
	int dotPos = fileName.ReverseFind(_T('.'));
	if (dotPos == -1 || dotPos <= lastUnderscorePos)
	{
		// 확장자가 없거나 "."이 "_"보다 앞에 있는 경우
		return _T("");
	}

	// "_" 다음부터 "." 앞까지의 문자열 추출
	return fileName.Mid(lastUnderscorePos + 1, dotPos - lastUnderscorePos - 1);
}

void CImageHashMngApp::SaveDictionaryToFile(const std::map<CString, CString>& dictionary, const CString& filePath)
{
	try
	{
		// 파일이 존재하면 삭제
		if (_taccess(filePath, 0) == 0) // 파일 존재 여부 확인
		{
			if (_tremove(filePath) != 0) // 파일 삭제
			{
				AfxMessageBox(_T("Failed to delete existing file."));
				return;
			}
		}

		// 파일을 열기 (UTF-8 인코딩으로 저장)
		std::ofstream outFile(filePath.GetString(), std::ios::out | std::ios::trunc | std::ios::binary);
		if (!outFile.is_open())
		{
			AfxMessageBox(_T("Failed to open file for saving dictionary."));
			return;
		}

		// UTF-8 BOM 추가 (선택 사항)
		const unsigned char utf8BOM[] = { 0xEF, 0xBB, 0xBF };
		outFile.write(reinterpret_cast<const char*>(utf8BOM), sizeof(utf8BOM));

#ifdef UNICODE
		using CStringT = CStringW; // 유니코드 문자 집합
#else
		using CStringT = CStringA; // ANSI 문자 집합
#endif

		auto ConvertCStringToUTF8 = [](const CStringT& str) -> std::string {
#ifdef UNICODE
			// UTF-16 -> UTF-8
			int utf8Len = WideCharToMultiByte(CP_UTF8, 0, str.GetString(), -1, nullptr, 0, nullptr, nullptr);
			if (utf8Len <= 0) {
				return std::string();
			}

			std::string utf8String(utf8Len, '\0');
			WideCharToMultiByte(CP_UTF8, 0, str.GetString(), -1, &utf8String[0], utf8Len, nullptr, nullptr);
			return utf8String;
#else
			// ANSI -> UTF-8
			int wideCharLen = MultiByteToWideChar(CP_ACP, 0, str, -1, nullptr, 0);
			if (wideCharLen <= 0) {
				return std::string();
			}

			std::wstring wideString(wideCharLen, L'\0');
			MultiByteToWideChar(CP_ACP, 0, str, -1, &wideString[0], wideCharLen);

			int utf8Len = WideCharToMultiByte(CP_UTF8, 0, wideString.c_str(), -1, nullptr, 0, nullptr, nullptr);
			if (utf8Len <= 0) {
				return std::string();
			}

			std::string utf8String(utf8Len, '\0');
			WideCharToMultiByte(CP_UTF8, 0, wideString.c_str(), -1, &utf8String[0], utf8Len, nullptr, nullptr);
			return utf8String;
#endif
			};

		// 딕셔너리 항목 저장
		for (const auto& entry : dictionary)
		{
			// CString을 UTF-8로 변환
			std::string key =  ConvertCStringToUTF8(entry.first);
			std::string value = ConvertCStringToUTF8(entry.second);

			// "key:value" 형식으로 저장
			outFile << key << ":" << value << "\n";
			outFile.flush(); // 강제로 플러시
		}

		outFile.close();
	}
	catch (const std::exception& e)
	{
		CString errorMessage;
		errorMessage.Format(_T("Error saving dictionary: %S"), e.what());
		AfxMessageBox(errorMessage);
	}
}

BOOL CImageHashMngApp::LoadDictionaryFromFile(const CString& filePath, std::map<CString, CString>& dictionary)
{
	try
	{
		// 파일 존재 여부 확인
		if (_taccess(filePath, 0) != 0)
		{
			AfxMessageBox(_T("HashDictionary.txt 파일이 존재하지 않습니다."));
			return FALSE;
		}

		// 파일 열기
		std::ifstream inFile(filePath.GetString(), std::ios::in | std::ios::binary);
		if (!inFile.is_open())
		{
			AfxMessageBox(_T("HashDictionary.txt 파일을 열 수 없습니다."));
			return FALSE;
		}

		// UTF-8 BOM 제거 (선택 사항)
		unsigned char bom[3];
		inFile.read(reinterpret_cast<char*>(bom), 3);
		if (bom[0] != 0xEF || bom[1] != 0xBB || bom[2] != 0xBF)
		{
			// 파일 시작 부분이 BOM이 아니면 다시 파일 포인터를 처음으로 설정
			inFile.seekg(0, std::ios::beg);
		}

#ifdef UNICODE
		using CStringT = CStringW; // 유니코드 문자 집합
#else
		using CStringT = CStringA; // ANSI 문자 집합
#endif

		auto ConvertUTF8ToCString = [](const std::string& str) -> CStringT {
#ifdef UNICODE
			// UTF-8 -> UTF-16
			int wideCharLen = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, nullptr, 0);
			if (wideCharLen <= 0) {
				return CStringT();
			}

			std::wstring wideString(wideCharLen, L'\0');
			MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, &wideString[0], wideCharLen);
			return CStringT(wideString.c_str());
#else
			// UTF-8 -> ANSI
			int wideCharLen = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, nullptr, 0);
			if (wideCharLen <= 0) {
				return CStringT();
			}

			std::wstring wideString(wideCharLen, L'\0');
			MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, &wideString[0], wideCharLen);

			int ansiLen = WideCharToMultiByte(CP_ACP, 0, wideString.c_str(), -1, nullptr, 0, nullptr, nullptr);
			if (ansiLen <= 0) {
				return CStringT();
			}

			std::string ansiString(ansiLen, '\0');
			WideCharToMultiByte(CP_ACP, 0, wideString.c_str(), -1, &ansiString[0], ansiLen, nullptr, nullptr);
			return CStringT(ansiString.c_str());
#endif
			};

		// 파일 라인 읽기
		std::string line;
		while (std::getline(inFile, line))
		{
			// "key:value" 형태로 파싱
			size_t delimiterPos = line.find(':');
			if (delimiterPos != std::string::npos)
			{
				std::string key = line.substr(0, delimiterPos);
				std::string value = line.substr(delimiterPos + 1);
				// CString으로 변환 후 딕셔너리에 저장
				dictionary[ConvertUTF8ToCString(key)] = ConvertUTF8ToCString(value);
			}
		}

		inFile.close();
	}
	catch (const std::exception& e)
	{
		CString errorMessage;
		errorMessage.Format(_T("Error loading dictionary: %S"), e.what());
		AfxMessageBox(errorMessage);
		return FALSE;
	}

	return TRUE;
}

CString CImageHashMngApp::GenerateFileHash(const CString& filePath)
{
	std::ifstream file(filePath.GetString(), std::ios::binary);
	if (!file.is_open())
	{
		return _T("");
	}

	// 파일 내용을 읽어 해시 생성
	std::vector<unsigned char> buffer(std::istreambuf_iterator<char>(file), {});
	unsigned char hash[SHA256_DIGEST_LENGTH];
	SHA256(buffer.data(), buffer.size(), hash);

	// 해시 결과를 CString으로 변환
	CString hashString;
	for (int i = 0; i < SHA256_DIGEST_LENGTH; ++i)
	{
		CString hex;
		hex.Format(_T("%02x"), hash[i]);
		hashString += hex;
	}

	return hashString;
}
