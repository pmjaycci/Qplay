Qplay Project
---
게임 플레이 영상 링크 : https://youtu.be/OL8IsQ9XXVg

---
작업자
---
* 서버 : 엄재창, 클라이언트 : 엄재창, 아트 : 엄재창

---
깃 주소
---
* 클라이언트 : https://github.com/QplayProject/Client
---
프로젝트 구성
---
* 서버
  ㄴ 로그인 서버
  ㄴ 채팅 서버
  ㄴ DB 서버
  
* C# ASP.Net Core API (HTTP)
- 로그인 서버와 채팅서버에 모두 사용 되었습니다.
* TCP Socket
  
* MySQL
---
Login Server
---
* Unary 방식 Grpc 구현  
  ㄴMutex Lock을 통해 동시성에 대한 순서 보장  
  
* Bidirect Stream 방식 Grpc 구현  
  ㄴList를 이용해 Queue 구현  
  ㄴ호출 들어온 메시지는 구현한 Queue에 담고 고루틴을 통해 Background에서 Queue에 담긴 데이터 메시지 처리하여 동시성에 대한 순서 보장

* RPC 기본 구조  
  ㄴProtoBuffer 구조는 코드 재활용성을 위해 string RpcKey, string Data(json marsharl data)로 이루어져 있다.  
  ㄴ서버 부팅시 map에 RpcKey와 RpcKey에 따른 함수를 캐싱하여 관리하며 서버에 해당 RpcKey에 따라 해당 함수를 호출하여 처리한다.

 
---
DataBase
---
* LoginDB, GameDB 서버에 오픈후 캐싱하여 관리
* LoginDB.item, item_weapon, item_effect, shop, shop_ingame 테이블의 경우 잦은 업데이트가 없기에 map으로 캐싱하여 관리
* LoginDB  
  ㄴAccount 테이블 user_name 컬럼을 FK로 연결하여 랭킹 테이블 user_name 컬럼과 연결 및 update cascade 처리  
  ㄴItem 테이블에는 모든 아이템 id 및 모든 아이템들의 공통된 컬럼들을 관리 및 item_weapon, item_effect 테이블에 FK로 연결 및 무기 또는 효과 아이템의 데이터 별도 관리  
* GameDB  
  ㄴInventory 테이블에 uid, item_id는 index 처리
* Redis
  ㄴHeartBeat 세션용

---
통신
---
* Notification Server와 Game Server(들)은 Bidirect Stream방식으로 연결되어 있다.
  ㄴNotification Server와 연결된 Game Server들은 Slice에 담고 연결이 끊어진 Game Server들은 Slice에서 제거한다.
  
* Login Server -> Notification Server
  ㄴLogin Server에서 Login 처리시 해당 유저가 HeartBeat값을 가지고 있을경우 중복 로그인으로 인식후 Notification에 Unary 방식으로 해당 유저 uid값과 함께 rpc호출

* Notification Server <-> Game Server
  ㄴNotification Server는 Slice에 담긴 연결된 Game Server들에게 HeartBeat, 중복 로그인 관련된 메시지를 보낸다.

* Game Server <-> Client
  ㄴGame Server와 연결된 Client들은 Slice에 담고 연결이 끊어진 Client들은 Slice에서 제거한다.  
  ㄴGame Server와 Client는 Bidirect Stream, Unary방식으로 연결되어 있다.  
  ㄴNotification Server로부터 들어온 메시지는 연결된 클라이언트들에게 Bidirect Stream방식으로 메시지를 보낸다.  
---
RPC List
---
**Game Server**
---
* load_tables : 캐싱된 DB테이블을 클라이언트에게 보낸다.  
  
* load_inventory : inventory테이블에서 인덱싱 되어있는 uid를 기준으로 탐색하여 나온 결과를 돌려준다.
  
* buy_item : 구매하고자 하는 아이템id를 클라이언트로부터 받게되고 캐싱 되어있는 shop테이블에서 아이템id를 키값으로 판매금액을 탐색후
  account테이블에서 재화에서 감소시킨뒤 잔액과 구매결과 여부를 돌려준다.
  
* upgrade_item : 강화하고자 하는 아이템id를 클라이언트로부터 받게되고 확률테이블에서 강화수치에 따른 확률정보를 가져와 랜덤함수로 비교하여 성공여부를 돌려준다.
  
* join_game : 착용하고 입장하고자 하는 무기 아이템id를 클라이언트로부터 받게되고 인게임용 재화, 아이템 착용 슬롯 정보 등을 초기화 후 map으로 캐싱후 해당 정보를 돌려준다.
  
* load_ingame_shop : 인게임 상점은 모든 아이템중 랜덤하게 4종류의 아이템을 판매하게 되는데  
  item테이블에 등록된 모든 id를 slice에 담고 0 ~ slice크기만큼의 범위중 무작위 값을 추출후 나온 값을 slice의 인덱스로 지정하고
  인게임 상점에서 판매하는 가짓수의 사이즈만큼 될때까지 재귀함수로 반복하여 slice값을 제거하여 돌려준다.
  이로하여 각 아이템들은 중복되지도 않으며 최대 item테이블에 등록된 아이템 갯수만큼만 반복 연산하는 것을 보장한다.
  
* buy_ingame_item : 구매하고자 하는 아이템id를 클라이언트로부터 받게되고 캐싱하여 들고있는 재화와 판매가격을 비교하여 구매 가능여부를 판별하고 구매 가능시 아이템 착용 슬롯 정보에 추가후 돌려준다.
  
* user_name : 등록하고자 하는 user_name값을 클라이언트로부터 받게되고 account테이블에 이미 등록되어있는지 비교후 결과 값을 돌려준다.
  
* load_time_attack_rank_table : 랭킹테이블에 등록된 정보를 돌려준다.  
  
* update_time_attack_rank : 타임어택 성공시 호출되는 Rpc로 클라이언트는 클리어한 시간을 서버에 전달하게 된다.  
  클리어 시간과 기존 랭킹 테이블에 저장된 클리어 시간과 비교하여 갱신하고 랭커의 경우 2배의 보상을 지급하고 클라이언트에게 돌려준다.
  
* game_over : 타임어택 실패시 호출되는 Rpc로 기본 보상을 처리하고 보상결과를 클라이언트에게 돌려준다.

**Login Server**
---
* login : 클라이언트로부터 받은 로그인 정보로 Redis에서 HeartBeat값을 가지고 있는지 판별후 HeartBeat값이 없을경우 로그인 성공  
  HeartBeat값이 있을경우 중복 로그인이라고 판단하고 Notification Server에 해당 유저의 로그인 정보를 보낸다.
  
**Notification Server**
---
* duplicate_login : Login Server로부터 받은 uid를 연결된 Game Server들에게 메시지로 전달한다.




# Server
#golang #gRPC
GOOS=linux GOARCH=amd64 go build -o game_server
