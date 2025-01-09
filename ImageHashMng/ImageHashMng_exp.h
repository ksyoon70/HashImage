#pragma once
#define exportFunc __declspec(dllexport)
extern "C" exportFunc BOOL InitEngine(TCHAR* strHashFilePath);			//hash 파일을 지정하여 읽어 들인다.
extern "C" exportFunc BOOL GenHash(TCHAR* strImagePath);				//이미지 폴더를 지정하여 hash 코드를 생성하여 hash 파일을 만들고 저장한다.
extern "C" exportFunc BOOL GetKey(TCHAR* strImagePath, TCHAR* key, size_t keyBufferSize);		//이미지 파일을 넣으면 key를 얻어오는 함수
extern "C" exportFunc BOOL FindValue(TCHAR* key, TCHAR* value, size_t valueBufferSize);			//key를 얻으면 value를 얻어 오는 함수.
extern "C" exportFunc BOOL CloseEngine();