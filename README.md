---
Qplay Project (작업 기간 3~4주)
---
* 게임 플레이 영상 링크 : https://youtu.be/OL8IsQ9XXVg
---
작업자
---
* 서버 : 엄재창, 클라이언트 : 엄재창, 아트 : 엄재창
---
깃 주소
---
* 클라이언트 : https://github.com/QplayProject/Client
---
로그인 서버
---
* C# ASP.Net Core API (HTTP)를 이용하여 서버를 구현하였습니다.
* 서버 부팅시 값이 변환되지 않는 상점, 아이템 테이블을 캐싱하게 됩니다.
* 해당 서버는 Task를 활용하여 비동기로 동작합니다.
* HTTP통신은 ApiController를 이용하여 주소에 따라 응답처리하게 하였습니다.
* 클라이언트 실행시 현재 버전을 로그인서버에 전달하게 되고 동일한 버전일 경우 캐싱된 상점, 아이템 테이블 값을 응답 처리 해줍니다.
* 로그인 서버에서 클라이언트에서 로그인 요청으로 ID/PW를 받게 되고 성공시 해당 유저의 닉네임 값을 응답 처리해주며 <br/>
클라이언트는 해당 닉네임 값으로 채팅 서버에 연결 요청을 합니다.
---
채팅 서버
---
* C# ASP.Net Core API (HTTP)통신 및 TCP 소켓 통신을 모두 이용하여 서버를 구현하였습니다.
* 서버 부팅시 값이 변환되지 않는 상점, 아이템 테이블을 캐싱하게 됩니다.
* C# ASP.Net Core API (HTTP)통신 및 TCP 소켓 통신 모두 Task를 활용하여 비동기로 동작합니다.
* TCP 소켓 통신은 연결된 클라이언트의 Opcode값을 비교하여 연산처리를 진행합니다.
* 요청 들어온 Opcode의 값이 JoinGame일 경우 해당 유저의 TcpClient는 Dictionary에 캐싱하여 관리합니다.
* 클라이언트로부터 들어온 메시지는 Queue에 담아 메시지를 처리하며<br/>
Dictionary에 캐싱된 TcpClient중 전달받을 유저들을 탐색후 그에 맞는 메시지를 보내게 됩니다.
* HTTP 통신은 Header값에 따라 어떤 호출인지 체크하며 유저의 상태 변경과 같이<br/>
상황에 따라 요청자 이외의 유저들에게도 보내야 하는경우 Dictionary에서 메시지를 보낼 유저를 탐색후 메시지를 보내게 됩니다.
---
DB 서버
---
* Database는 UserDB, TableDB가 존재합니다.
* TableDB에는 Item, Shop 테이블을 가지고 있으며 서버에서 캐싱할 테이블들을 가져올때 이용됩니다.<br/>
Item테이블과 Shop테이블은 ItemId를 키값으로 관계형으로 형성되어있습니다.
* UserDB에는 Account, Inventory 테이블을 가지고 있으며 로그인, 유저 정보등을 가져올때 이용됩니다.<br/>
Account 테이블에서는 탐색시 필요한 UserName을 PK로 가지고 있습니다.
Inventory 테이블에서는 테이블 ID가 PK로 가지고 있으며 UserName과 ItemId는 인덱싱 처리하여 탐색에 용이하게 처리하였습니다.
---
마치며
---
제가 가장 좋아하는 게임이었고 포토샵을 통해 UI이미지 만드는것부터 클라이언트며 서버며 모두 작업하느라 힘은 들었지만<br/>
기능 하나씩 구현할때마다 짜릿함을 느꼈습니다.<br/>
이번 프로젝트의 목표로는 게임서버에서 비동기 처리, 동시 요청 처리, 공통 기능 래핑 처리 크게 이 세가지를 목표로 두고 작업했습니다.<br/>
DB테이블의 경우 현 프로젝트 규모가 작기에 좀더 분산하여 Database는 나누지 않았으며<br/>
유저의 세션을 관리할 Redis서버, Notification서버 또한 처음 기획하고 잡았던 기간내에 볼륨에는 해당하지 않아 구현하지 않았습니다.<br/>
감사합니다.

