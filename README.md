# 📌 Information 
### ✨ Introduce
```
A scanner program that detects servers (devices) on the BACnet network and stores sub-object query results in the DB.
```

<br><br><br>
### ✨ Version
```
- Language: C#(.Net 4.8)
- Library:
  - BACnet: 3.0.1
  - SQLite: 1.0.119
```

<br><br><br>

### ✨ Logic
```
1. Arguments로 IP, Port를 입력받는다.
2. 입력받은 네트워크를 대상으로 Who-Is 패킷을 브로드한다.
3. I-Am 패킷으로 응답한 백넷 서버를 기록한다.
    * 1분간 응답없을 시 프로그램 종료
4. 인터벌마다 응답이 기록된 서버를 대상으로 오브젝트를 조회한다.
5. 조회된 오브젝트를 DB 파일에 저장한다.
```
