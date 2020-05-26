# COM3D2.ScriplayLilly2.Plugin

# 소개

## 원본

- https://ux.getuploader.com/arch_plugin/download/2
- Scriplay_0.2.0_COM3D2GP-01Ver.1.27.1x64.zip

위 파일의 개인적 취향의 개조 버전

## 내용

- 컨트롤 + 쉬프트 + R로 창위치 재설정
- "COM3D2\PhotoModeData\MyPose" 위치의 anm 파일을 읽을수 있도록 수정
- @motion name=폴더경로\ 형식 추가. 
@motion name=test\
라고 입력할경우
"COM3D2\PhotoModeData\MyPose\test" 폴더 내의 모든 모션 파일중 랜덤 실행 
- anm 파일명이 게임파일과 같은 이름일경우 로컬 우선

- 그외는 원본이 워낙 잘 만들어져서 특별히 손댈게 없던...

## 샘플 스크립트

아래의 스크립트 참조 (그로계 주의)
https://github.com/customordermaid3d2/COM3D2.ScriplayLilly2.Plugin/releases/download/20200129/COM3D2_Scriplay.7z

- 모션 스크립트
https://youtu.be/rSmpwVIO4-U
- 소리 스크립트
https://youtu.be/wC7AG-CxULY

## 이하 아래는 원문을 번역기 돌린거


# COM3D2.Scriplay.Plugin

# 소개

COM3D2위한 자체 스크립트 (마크 다운과 KAG 스크립트를 맞춘 같은 표기법)에서 가정부의 움직임을 설명 할 수있는 MOD입니다. 마크 다운 편집기에서 작성 · 열람하십시오.



지금까지 가정부에 약간의 동작 (모션 음성 표정 등)을하고자하는 경우에도 다음과 같은 어려움이있었습니다.

- MOD 작성이 필요한 환경 구축 등의 문턱이 높고 어려운
- MOD를 수정 한 후 게임을 다시 시작해야 반복 작업이 매우



그래서 자신 스크립트 메이드 동작의 설명과 게임 중에서 스크립트를 다시로드 할 수 있도록했습니다.



# 이용시주의 사항

본 MOD는 아직 버그도 많고 매번 게임이 충돌 할 수도 있습니다. 본 MOD를 사용 할 때마다 게임이 크래쉬해도 ​​괜찮도록 미리 세이브를 해 두는 것이 좋습니다.

또한, 본 MOD를 이용하는데있어서 문제가 발생하더라도 저자는 책임을지지 않습니다. 자기 책임으로 이용하십시오.



# 동작 환경

사전에 다음 MOD / dll을 소개합니다.

- しばりす 2
- ExPluginBase (PluginExt.dll) v0.3
  - asm256 씨 숙박 작성 https://github.com/asm256/ExPluginBase/releases에서 입수


※ COM3D2 GP-01 Ver.1.27.1 x64에서 동작 확인

# 설치 방법

1. 위의 "しばりす 2"를 도입하고 UnityInjector가 동작하는 환경을 만들기
2. 위의 "PluginExt.dll"을 "Sybaris \ UnityInjector"폴더에 복사
3. "UnityInjector"폴더를 "Sybaris"폴더에 복사
4. "Config"폴더를 "Sybaris \ UnityInjector"폴더에 복사
5. 게임 시작 후 콘솔에 "`Loaded Plugin : 'Scriplay. *. *. *'` '로 표시되는 것을 확인



# 간단한 사용법 ~ 스크립트를 실행하려면

본 MOD 스튜디오 모드에서의 이용을 상정하고 있습니다.



샘플 스크립트의 실행 방법

1. 스튜디오 모드 시작
2. 메이드을 2 명 배치
3. 화면 우측 하단의 "UI OFF"를 선택하여 Scriplay 이외의 창을 끌
4. 스크립트 목록에서 실행하려는 스크립트를 선택한다.

# 게임의 조작 화면

## Config 화면

스크립트 작성에있어서 간이 적으로 음성이나 모션 등을 확인할 수 있습니다.

### prop 복원

prop는 가정부 옷을 입히거나, 도구를 갖게 할 때 사용하는 기능입니다.

prop를 취소하고 가정부를로드 직후의 모습으로합니다.

### 음성 재생

음성 파일 이름 ( "H0_08095" "H0_08095.ogg '등)을 지정하면 해당 음성을 재생합니다.

재생 시작 위치 (초)을 지정하면 음성이 재생 위치를 변경할 수 있습니다.

재생 시간 (초)을 지정하면 음성의 재생 시간을 변경할 수 있습니다.

최근 3 개까지 재생 된 음성은 버튼에서 다시 실행할 수 있습니다.

### 모션 재생

모션 이름 ( "kaiwa_tati_taiki_f" "kaiwa_tati_taiki_f .anm '등)을 지정하면 해당 모션을 재생합니다.

최근 3 개까지 재생 모션은 버튼에서 다시 실행할 수 있습니다.

### 표정 재생

표정 이름 ( "생긋」등)을 지정하면 해당 표정을 재생합니다.

최근 3 개까지 재생 한 표정은 버튼에서 다시 실행할 수 있습니다.

### prop 전환

prop 파일 이름 ( "handitemr_racket_i_" "handitemr_mugcup_i_ .menu '등)을 지정하면 해당 prop를 장착합니다.

### 스크립트 실행

한 줄 정도의 짧은 스크립트 ( "@show text = 테스트 코멘트 표시 wait = 3s" "@ motion name = kaiwa_tati_syazai_taiki_f '등)을 실행합니다.


### 카탈로그 생성

"가정부를 자동 조작하여 화면 캡처"를 반복하여 카탈로그가되는 HTML 파일을 생성합니다. HTML 파일은 "`\ Sybaris \ UnityInjector \ Config \ Scriplay \ capture`"폴더에 생성되어 HTML 링크 이미지 "`\ Sybaris \ UnityInjector \ Config \ Scriplay \ capture \ image`"폴더에 저장됩니다. 이미지는 게임의 전체 화면 캡처하므로 그대로라고 볼 어렵다고 생각합니다. 그에 따라 이미지 일괄 편집 소프트웨어 메이드 만 자르기 등 편집하십시오.

#### 카탈로그 생성시주의 사항

- 제대로 화면 캡처하기 위해 카탈로그 생성 중 게임 화면을 활성화해야합니다.
- 강제 종료하고 싶을 때는 "종료"버튼을 누르십시오.
- 실행하는 데 시간이 걸리기 때문에 자기 전에 실행 등을 추천합니다.



#### 생성 가능한 카탈로그

- 표정 카탈로그
- 메이드 모션 카탈로그
- 주인님의 모션 카탈로그
- 메이드 prop 모션 카탈로그



## VR로 스크립트 실행

VR 모드에서는 조작 태블릿 아래 스크립트 목록이 표시되며, 여기에서 스크립트 실행이 가능합니다. HTC-VIVE에서만 동작 확인하고 있습니다. (아마 다른 VR 환경에서도 동작 할 것).

### 조작 방법

컨트롤러를 카메라 모드에서 방향키로 다음과 같이 조작합니다.

- ↑ ↓ : 커서 이동
- → : 확정
  - 스크립트의 선택
  - 선택 선택
- ← 길게
  - 스크립트 목록보기 : 스크립트 다시 읽기
  - 스크립트 실행 중 : 정지



# 스크립트의 개요

스크립트는 실행 스크립트 작성 스크립트 파일 (`* .md`)과 스크립트에서 참조 할 수있는 리소스 테이블 (표정이나 음성에 카테고리 이름을 붙여 쉽게 접근하는 것)를하는 자원 테이블 파일 (`* .csv`)에서를 작성하여 스크립트를 만듭니다.



## 스크립트 파일 (`* .md`)

다음 폴더에 배치 된 마크 다운 파일 (* .md)는 게임 중에서 스크립트로 읽기 · 실행할 수 있습니다. 게임 실행 중에 다시로드 가능. 파일은 UTF-8 인코딩한다.

```
Sybaris \ UnityInjector \ Config \ Scriplay \ scripts
```



### 라이브러리 (`lib _ *. md`)

파일 이름이 "`lib _ *. md`"에 해당하는 파일 ( "lib_general.md"등)는 라이브러리로서 다루어 져 스크립트를 실행할 때 자동으로로드됩니다. 많은 스크립트에서 사용하는 일반적인 서브 루틴이나 전역 변수처럼 취급하고 변수가있을 때는 라이브러리에두면 좋다.

## 자원 테이블 파일 (`* .csv`)

다음 폴더에 배치 된 특정 접두사가 붙은 CSV 파일 (* .csv) 범주 정의 파일로 처리됩니다. 게임 실행 중에 다시로드 가능. 파일은 UTF-8 인코딩한다.

```
Sybaris \ UnityInjector \ Config \ Scriplay \ csv
```



아래 표 접두사가 붙은 CSV 파일은 모두 읽혀 카테고리가 정의됩니다.

| 접두사 | 정의 | 파일 이름의 예 |
| : -------- : | : ------------------------------------ : | : ------------------- : |
| loopvoice_ | 루프 음성 (반복 발성하는 음성) | loopvoice_default.csv |
| oncevoice_ | 원 보이스 (한 번만 발성하는 음성) | oncevoice_default.csv |
| face_ | 표정 | face_default.csv |



카테고리에는 스크립트에서 액세스 할 수 있습니다.

예를 들어, 루프 음성 범주는 다음과 같이 호출 할 수 있습니다.

```
@talkRepeat maid = 0 category = 보통 0
```





## 스크립트 동작의 개요

- 1 번째 줄부터 순서대로 실행
- 구문 명령으로 해석 할 수없는 행은 건너 뛰
- 1 프레임에 1 명령 실행
  - 멀티 스테이트먼트의 경우는 1 행에 기술 한 모든 명령이 실행된다.
- 오류 등은 콘솔에 출력된다



## 명령 개요
- 대부분의 명령은`@ (명령어) (매개 변수 이름) = (값)`의 형식으로 기술되는
- 각 명령에는 매개 변수가 지정 가능
  - (매개 변수 이름) = (매개 변수 값)로 작성
    - "="사이에 공백 등 넣지 않도록주의!
  - 공백 or 탭 문자로 여러 매개 변수 설명 가능
- 명령어 매개 변수 이름 모두 모두 소문자로 고쳐에서 해석되는
  - 예를 들어,`@ talkrepeat`와`@ talkRepeat` 같은 명령
- 파라미터의 지정 순서는 상관 없음. 교체하거나해도 결과는 같다.
- 예 :`@pos maid = 0 x = 0 y = 0 z = 0`





## 많은 명령에 공통 매개 변수에 대해

- "maid"매개 변수
  - 명령 작업 대상의 가정부를 지정하는 정수.
    - (스튜디오 모드) 가정부를 선택한 순서대로 "0, 1, 2, ..."라고 번호 부여
    - 예 :`@face maid = 0 name = 멍하니`
  - "지정되지 않음"또는 "`all`로 지정했다"때 모든 가정부가 조작 대상이되는
    - 예 : 전원이 생긋하기
      -`@face name = 활짝`
      -`@face name = 생긋 maid = all`
- "man"매개 변수
  - 명령 작업 대상의 주인님을 정수로 지정.
  - 사용법은 "maid"매개 변수와 거의 같다.
  - 그러나 주인님은 실행할 수없는 명령도 있으므로주의 (표정 재생, 음성 재생 등).
- "name" "category"매개 변수
  - 한가지 방법으로 동작 내용을 지정
    - "name"라면 리소스 이름을 지정
      - (모션 파일명 표정 이름 등)
    - "category"라면 카테고리 이름을 지정
      - 카테고리는 여러 자원 이름을 정리해 하나의 범주로 정의합니다
      - 카테고리는 CSV 파일에서 정의
- "wait"매개 변수
  - 명령 실행시의 대기 시간을 소수점 값
    - 예 :`@show text = 잠깐 표시 wait = 0.9s`
  - 단위는 붙여도 붙이지 않아도된다
    - 모두 OK "wait = 3sec" "wait = 3s" "wait = 3"
  - 시간 경과 후 (명령 실행 중에도) 다음 줄이 시작된다
- "fade"매개 변수
  - 천천히 변화 할 때 전환 시간 (초)을 소수점 값
    - 예 1 :`@motion name = kaiwa_tati_tere_taiki_f fade = 0.1s`
    - 예 2 :`@motion name = kaiwa_tati_tutorial_2_taiki_f fade = 5s`
  - 단위는 붙여도 붙이지 않아도된다


## 구문 관련

### 라벨

"#"로 시작하는 줄은 레이블로 인식된다.

"@goto"명령에서 점프 · "@call"명령의 호출을 시도하는.

> 마크 다운 편집기에서 스크립트를 볼 때 머리글로 표시되고 편집기의 "문서 구조"와 "개요"기능 목록 수 있으므로 스크립트의 처리 단위마다 라벨을 붙여두면 좋다.



### 코멘트

"//"로 시작하는 줄은 주석으로 인식되고 실행되지 않는다.

> 해석 할 수없는 행의 차이는 콘솔에 해석 할 수 없다는 오류 표시되는지 여부.



## 변수

`$ (변수 이름) = (값)`에서 변수 정의된다. 값은 문자열로 처리된다.
`$ {(변수 이름)}`에서 변수를 호출 할 수있다. 호출 변수가 적힌 줄이 실행되기 전에 변수의 부분 값으로 대체하고 명령이 평가되도록되어있다.

예 :
```
// 변수 정의
$ motionName = kaiwa_tati_yo_taiki_f
// 변수 호출
@motion name = $ {motionName}
```

## 다중 문

"; (세미콜론)"로 구분하여 한 줄에 여러 명령을 열거 할 수있다. "`@ wait`"등 일시 정지 계의 작업은 잘 작동하지 않을 수 있으므로주의.

예 :

```
@selection
- 기뻐 exec = @ motion name = kaiwa_tati_yorokobub_f_once_; @talk name = H0_01954
- 두드리는 exec = @ sound name = slap_high; @talk name = H1_02019

@wait 3s

```





## 서브 루틴

`@call (레이블 이름)`이 배치 파일처럼 라벨 시설을 서브 루틴으로 호출 할 수있다. 지정된 레이블로 점프 후`@ exit`이 실행될 때 호출로 처리가 돌아온다.

서브 루틴으로 호출 레이블은 마지막에`@ exit`에서 끝나도록주의한다.



### 초기화 루틴

"`init_`"로 시작하는 레이블은 초기화 처리의 서브 루틴으로 인식 된 스크립트를 읽어 들인 후 자동으로 실행된다.



## 스크립트의 실행 순서

스크립트 실행 후 다음의 순서로 실행된다.

1. 라이브러리 (`lib _ *. md`)로드
2. 선택한 스크립트 (`* .md`)로드
3. 모든 레이블의 행 번호를 기록
4. 초기화 루틴 (`init_`로 시작 레이블)의 실행
5. 선택한 스크립트의 첫 번째 행에서 처리 시작



## Scriplay 좌표계

스크립트 실행을위한 좌표계 (Scriplay 좌표계)를 정의하고, 거기에서 상대 값으로 가정부와 카메라의 위치를 ​​지정할 수있다.

! [image-20191129214157993] (readme.assets / image-20191129214157993.png)





# 명령 참조

명령의 목록과 자세한 사용 방법.

실제로 스크립트를 만들 때 동봉 된 샘플 스크립트에서 복사 - 붙여 넣기하는 것이 빠를지도.







## 설정 시스템 명령

### @selection

선택 버튼을 표시한다. 옵션 버튼을 선택한 경우의 처리도 설명 할 수있다.



작성 형식

```
@selection (매개 변수)
- (옵션에 표시하는 문자) goto = (점프)
- (옵션에 표시하는 문자) call = (호출 서브 루틴)
- (옵션에 표시하는 문자) exec = (수행 할 작업)

```





| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| wait | 무작위 | 옵션을 표시 한 채 처리를 일시 정지하는 시간 (초)입니다. 옵션이 선택되지 않고 지정된 시간이 지나면 @selection 이후의 처리를 재개한다. <br /> 기본값 : 1 년 (즉, 선택 버튼을 표시 한 채 계속 일시 중지합니다. 옵션이 선택 될 때까지 처리가 일시 정지) |
| keep | 무작위 | 1 : 선택 이후에 스크립트 처리를 진행할 경우 옵션을 선택할 수있는 상태 표시 해둔다. <br /> 0 : 선택 이후에 스크립트 처리를 진행 한 경우는 선택을 숨기기 <br /> 기본값 : 0 |
| mode | 모든 | gotolist : 실행중인 스크립트의 모든 레이블에 대해 이동 수있는 선택 버튼을 끝에 추가한다. 스크립트를 디버깅 할 때 유용한 기능입니다. <br /> none :( 표준 행동) <br /> 기본값 : none |



예 1)

```
@selection wait = 3s
- 선택1 goto= 레이블1
- 선택2 exec= @show text=선택2가 선정되었습니다.
@exit
# 레이블1
@show text=선택1가 선정되었습니다.
@exit
```



### @show

텍스트를 표시한다. 텍스트보기는 처리는 일시 정지한다.

| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| text | 필수 | 표시 할 텍스트를 지정 |
| wait | 무작위 | 텍스트를 표시하는 시간 (초) <br /> 기본값 : 표시 할 텍스트의 길이에 따라 자동으로 결정. |



예 1 : "안녕하세요"라고 표시하고, 처리를 일시 중지합니다. 잠시 후 처리를 재개하고 다음 줄이 실행되고 "안녕"이라고 표시된다.

```
@show text = 안녕하세요
@show text = 안녕
```



예 2 : "안녕하세요"라고 표시하고, 처리를 5 초 동안 일시 중지합니다. 잠시 후 처리를 재개하고 다음 줄이 실행되고 "안녕"이라고 표시된다.

```
@show text = 안녕하세요 wait = 5s
@show text = 안녕
```


### @origin

Scriplay 좌표계의 원점의 위치 · 방향을 정의한다.

| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| maid | 모든 | Scriplay 좌표계의 원점을 지정한 메이드의 위치 · 방향과 동일하게 정의한다. <br /> 기본값 : 지정되지 않음 |
| x | 모든 | Scriplay 좌표계의 원점의 x 좌표를 월드 좌표계를 지정합니다. <br /> 기본값 : Scriplay 좌표계 원점의 현재 값입니다. |
| y | 모든 | Scriplay 좌표계의 원점의 y 좌표를 월드 좌표계를 지정합니다. <br /> 기본값 : Scriplay 좌표계 원점의 현재 값입니다. |
| z | 모든 | Scriplay 좌표계의 원점의 z 좌표를 월드 좌표계를 지정합니다. <br /> 기본값 : Scriplay 좌표계 원점의 현재 값입니다. |
| rx | 모든 | Scriplay 좌표계의 방향을 좌표계의 x 축 주위 방향으로 지정. <br /> 기본값 : Scriplay 좌표계 원점의 현재 값입니다. |
| ry | 모든 | Scriplay 좌표계의 방향을 좌표계의 x 축 주위 방향으로 지정. <br /> 기본값 : Scriplay 좌표계 원점의 현재 값입니다. |
| rz | 모든 | Scriplay 좌표계의 방향을 좌표계의 x 축 주위 방향으로 지정. <br /> 기본값 : Scriplay 좌표계 원점의 현재 값입니다. |



예 1 :

Scriplay 좌표의 원점을 좌표계로 지정

```
@origin x = 1 y = 1 z = 1 rx = 0 ry = 0 rz = 0
```



예 2 :

Scriplay 좌표의 원점을 만든 위치 0의 위치 · 회전축에 세트

```
@origin maid = 0
```



### @goto

지정된 레이블 행으로 이동하여 거기에서 처리를 재개한다.



기술 형식 :

```
@goto (레이블 이름)
```



예 1 :

```
@goto 라벨 1
@show text =이 줄은 실행되지 않는다

# 레이블 1
@show text =이 줄은 실행된다.

```



### @call

지정된 라벨을 서브 루틴으로 호출한다.

(즉, 라벨의 행으로 이동하여 거기에서 처리를 재개하고`@ exit`이 실행되면`@ call` 실행 된 행까지 돌아와.)



기술 형식 :

```
@call (레이블 이름)
```



예 1 :

```
@call 라벨 1
@show text =이 줄은 두 번째로 실행되는
@exit

# 레이블 1
@show text =이 행은 1 번째로 실행되는
@exit
```



### @exit

`@ call`에서 호출 된 서브 루틴에서 실행하면 호출자 가기.

그렇지 않은 경우 스크립트를 종료한다.



### @wait

지정된 시간 (초) 만 처리를 일시 중지합니다. 단위는`sec``s` (없음) 중도 좋다.



기술 형식 :

```
@wait 3sec
```


### @soundrepeat

음향 효과 계의 음성을 반복 재생한다.

| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| name | 필수 | 재생하는 오디오 파일 이름을 지정 <br />`stop`을 지정하면 음성 정지. <br /> 또한 아래 표의 값을 지정하면 각각 대응하는 음성을 재생한다. |



| 설정 | 재생하는 오디오 |
| ---------- | -------------- |
| vibe_low | 약한 진동 소리 |
| vibe_high | 강하게 진동 소리 |
| kuchu_low | 약한 쿠츄 소리 |
| kuchu_high | 강하게 쿠츄 소리 |
| slap_low | 약하게 치는 소리 |
| slap_high | 강하게 치는 소리 |



예 1 : 약한 진동 소리를 3 초 재생 한 음성을 중지합니다.
```
@soundrepeat name = vibe_low
@wait 3s
@soundrepeat name = stop
```



### @sound

음향 효과 계의 음성을 한 번만 재생합니다. 매개 변수 등은`@ soundrepeat`과 같다.



예 1 : 약하게 치는 소리를 재생

```
@sound name = slap_low

```



### @config





| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| maid | 무작위 | 동작 대상 메이드을 만든 번호로 지정 |
| man | 무작위 | 작동 할 남자를 번호로 지정 |
| seikaku | 무작위 | 지정한 메이드의 성격을 임시 설정한다. 음성을 카테고리로 지정한 경우 지정된 성격에 대응 한 음성으로 재생되게된다. 스크립트 끝에 만든 성격 임시 설정은 해제된다. <br /> 가능한 값 : Pride / Cool / Pure / Muku / Majime / Rindere <br /> 기본 :( 지정 없음) |
| visible | 무작위 | 1 : 지정한 메이드을 표시한다. <br /> 0 : 지정한 메이드을 숨길한다. <br /> 기본 :( 지정 없음) |
| restore | 무작위 | 1 : 스크립트 종료시 (명령 종료시가 아닌) 모든 제작 Prop 모두 초기 상태로 복원하도록 예약한다. <br /> 0 : 스크립트 종료시 Prop 복원 예약을 해제한다. <br /> 기본 :( 지정 없음) |



### @background

배경을 변경한다.



| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | -------------------------- |
| name | 필수 | 어떤 배경으로 변경할지 여부를 지정 |

name에 가능한 값 :

| 값 |의 배경 |
| ------------ | ------ |
| Penthouse | 펜트 하우스 |
| Shitsumu_ChairRot | 집무실 |
| Shitsumu_ChairRot_Night | 집무실 (밤) |
| Salon | 살롱 |
| Syosai | 사무실 |
| Syosai_Night | 서재 (밤) |
| DressRoom_NoMirror | 드레스 룸 |
| MyBedRoom | 자기 방 |
| MyBedRoom_Night | 자기 방 (밤) |
| HoneymoonRoom | 허니문 룸 (밤) |
| Bathroom | 욕실 (밤) |
| PlayRoom | 놀이방 |
| PlayRoom2 | 놀이방 2 |
| Pool | 수영장 |
| SMRoom | SM 룸 |
| SMRoom2 | 지하실 |
| Salon_Garden | 정원 |
| LargeBathRoom | 목욕탕 |
| OiranRoom | 기녀 방 |
| Town | 도시 |
| Kitchen | 주방 |
| Kitchen_Night | 주방 (밤) |
| Salon_Entrance | 현관 |
| poledancestage | 폴 댄스 |
| Bar | 바 (밤) |
| Toilet | 화장실 |
| Soap | 비누 |
| MaidRoom | 하인방 |



예 1 : 배경을 하인방 변경

```
@background name = MaidRoom 하인방
```

### @camera

카메라의 관찰 대상을 지정하여 카메라의 위치 · 방향을 변경한다. 관찰 대상을 메이드로 지정한 경우는 제작의 정면, d 매개 변수로 지정된 거리로 카메라가 이동합니다. 관찰 대상을 좌표로 지정한 경우에는 지정된 좌표에서 d 매개 변수로 지정된 거리로 카메라가 이동한다.

| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| maid | 무작위 | 지정한 만든 이전 카메라를 이동 <br /> 지정하지 않으면 카메라의 좌표를 매개 변수로 직접 지시한다. <br /> 기본 :( 지정 없음) |
| x | 무작위 | 카메라의 X 위치를 Scriplay 좌표계로 지정 (단위 : 미터). <br /> 기본값 : 현재 값 |
| y | 무작위 | 카메라의 y 위치를 Scriplay 좌표계로 지정 (단위 : 미터). <br /> 기본값 : 현재 값 |
| z | 무작위 | 카메라의 z 위치를 Scriplay 좌표계로 지정 (단위 : 미터). <br /> 기본값 : 현재 값 |
| rx | 무작위 | 카메라의 X 축 주위 방향을 Scriplay 좌표계로 지정 (단위 :도). <br /> 기본값 : 현재 값 |
| ry | 무작위 | 카메라의 y 축 주위 방향을 Scriplay 좌표계로 지정 (단위 :도). <br /> 기본값 : 현재 값 |
| rz | 무작위 | 카메라의 z 축 주위 방향을 Scriplay 좌표계로 지정 (단위 :도). <br /> 기본값 : 현재 값 |
| d | 무작위 | 카메라와 대상의 거리를 지정 (단위 : 미터). <br /> 기본값 : <br /> 메이드을 대상으로 카메라 이동하는 경우는 1.5 <br /> 위치 · 방향 지정에서 카메라 이동하려면 0 |


예 1 : 1 번째 만든 정면 1.5m 떨어진 곳에 카메라를 이동한다.

```
@camera maid = 0
```





예 2 : Scriplay 좌표계에서 (0, 1.5, 0)의 점에 대해서, 정면에서 2m 떨어진 곳에 카메라를 이동한다. (예를 들어, 신장 1.5m의 메이드가 Scriplay 좌표계에서 (x = 0, z = 0)의 곳에 있었다면 그 제작을 정면에서 비추는 같은 위치로 카메라가 이동한다.)

```
@camera x = 0m y = 1.5m z = 0m ry = 180deg rx = 0deg d = 2m
```



예 3 : Scriplay 좌표계 (1,1,1) 점에 30 번 올려다 같이 0m 떨어진 곳에 카메라를 이동한다. (즉, Scriplay 좌표계 (1,1,1)의 점으로 카메라를 이동하여 30 번 고개 방향으로한다.)

```
@camera x = 1m y = 1m z = 1m ry = 0deg rx = -30deg
```



예 4 : Scriplay 좌표계 (1,1,1) 점에 30 번 올려다 있도록 1m 떨어진 곳에 카메라를 이동한다.

```
@camera x = 1m y = 1m z = 1m ry = 0deg rx = -30deg d = 1m
```



### @fadein

화면을 흐리게한다.

| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------ |
| fade | 무작위 | 암전 시간 (초)을 지정합니다. <br /> 기본값 : 0.5 초 |



예 1 : 화면을 흐리게한다.

```
@fadein
```



### @fadeout

화면의 암전을 해제하여 게임 화면이 보이게한다.

매개 변수는`@ fadein`과 같다.



예 1 : 화면의 암전을 해제한다.

```
@fadeout
```



### @info

스크립트에 대한 정보를 기술한다.

| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| info | 무작위 | 스크립트 버전을 기술한다. 스크립트 버전의 설명이없는 경우 자동으로 최신 버전의 스크립트 판별하고 실행한다. <br /> 기본 :( 지정 없음) |



예 1 : 스크립트 버전 2임을 스크립트 파일에 나타낸다.

```
@info version = 2
```



### @bgm

BGM을 변경한다.

| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- |
| name | 필수 | 변경 BGM 파일 이름을 지정 |

예 1 : BGM을 변경한다.

```
@bgm name = bgm007
// "@ bgm name = bgm007.ogg」에서도 같은
```



### @require

스크립트의 실행에 필요한 요구 사항을 기술한다. 요구 사항에 부합하지 않을 경우, 부합하지 않는 요구 사항을 화면에 크게 표시하고 스크립트를 종료한다.

| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| maidNum | 무작위 | 스크립트의 실행에 필요한 최소 메이드의 수를 지정 <br /> 기본 :( 지정 없음) |
| manNum | 무작위 | 스크립트의 실행에 필요한 최소 남자의 수를 지정 <br /> 기본 :( 지정 없음) |



예 1 : 메이드가 2 명 이상 배치되어 있지 않으면 스크립트를 종료한다.

```
@require maidNum = 2
```



## 메이드 조작계 명령

### @pos

Scriplay 좌표계에서 만든 위치 · 방향 (도)를 지정합니다.



| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| maid | 무작위 | 동작 대상 메이드을 만든 번호로 지정 |
| man | 무작위 | 작동 할 남자를 번호로 지정 |
| x | 무작위 | 만든 x 좌표를 좌표로 지정 (미터 단위) <br /> 기본값 : 현재 값. |
| y | 무작위 | 만든 y 좌표를 좌표로 지정 (미터 단위). <br /> 기본값 : 현재 값. |
| z | 무작위 | 만든 z 좌표를 좌표로 지정 (미터 단위). <br /> 기본값 : 현재 값. |
| rx | 무작위 | 만든 x 축 주위 방향을 Scriplay 좌표계로 지정 (도 180에서 반전). <br /> 기본값 : 현재 값. |
| ry | 무작위 | 만든 y 축 주위 방향을 Scriplay 좌표계로 지정 (도 180에서 반전). <br /> 기본값 : 현재 값. |
| rz | 무작위 | 만든 z 축 주위 방향을 Scriplay 좌표계로 지정 (도 180에서 반전). <br /> 기본값 : 현재 값. |



예 1 :

첫 번째 메이드을 Scriplay 좌표계의 x = 1 [m]의 위치로 이동한다. Scriplay 좌표계에 대해 90도 오른쪽을 향한다. y, z 좌표는 그대로.

```
@pos maid = 0 x = 1 ry = 90
```


### @face

지정한 메이드의 표정을 변경한다.



| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------------------ | ----------------- ------------------------------------------- |
| maid | 무작위 | 동작 대상 메이드을 만든 번호로 지정 |
| name | name 또는 category 필수 | 재생하는 표정 이름을 지정 |
| category | 〃 | 재생하는 표정 카테고리를 지정 |
| fade | 무작위 | 표정의 변경 시간 (초)을 지정합니다. <br /> 기본값 : 1 초 |
| hoho | 무작위 | 부끄러워도를 0-3의 정수로 지정 (지정하지 않음) : 현상 유지 |
| namida | 무작위 | 눈물의 양을 0-3의 정수로 지정 (지정하지 않음) : 현상 유지 |
| yodare | 무작위 | 0 : 침없이 1 : 잠꼬대 있습니다 (지정하지 않음) : 현상 유지 |
| eye | 무작위 | 지정 값만 두 눈을 위로 향하게. 값은 0 ~ 1의 범위에서 지정한다. <br /> 기본 :( 지정 없음) |



예 1 : 1 번째 메이드 대해 3 초에 걸쳐, 표정을 "피식"로 변경하고, 뺨의 붉은 3에 눈물을 3에 침을가한다.

```
@face maid = 0 name = 생긋 hoho = 3 namida = 3 yodare = 1 fade = 3sec
```



예 2 : 첫 번째 메이드 대해 0 초에 걸쳐 (즉 순간적으로) 표정을 자원 테이블 파일에 지정된 "보통 0"카테고리 중 하나로 지정하고, 뺨의 붉은 0에 눈물 0에 침을없이한다.

```
@face maid = 0 category = 보통 0 hoho = 0 namida = 0 yodare = 0 fade = 0sec
```



예 3 : 두 번째 메이드 대해 눈을 약간 위로 향하게.

```
@face maid = 1 eye = 0.5
```



### @motion

지정한 만든 모션을 변경한다.

모션 이름에`_once_`가 포함 된 경우 해당`_taiki_` 모션을 찾아오고, once 모션 재생 후 자동으로 taiki 모션을 재생한다.

예 :`@motion name = kaiwa_tati_yorokobub_f_once_`



| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| maid | 무작위 | 동작 대상 메이드을 만든 번호로 지정 |
| man | 무작위 | 작동 할 남자를 번호로 지정 |
| name | 필수 | 재생 모션 이름을 지정 |
| finish | 무작위 | 1 : 모션 재생이 끝날 때까지 스크립트를 일시 중지 |
| speed | 무작위 | 모션 재생 속도를 소수점 값 <br /> 기본값 : 1 |
| afterSpeed ​​| 모든 | once 모션 재생 후 taiki 모션 재생 속도를 소수점 값 <br /> 기본값 : 1 |
| fade | 무작위 | 모션 전환 시간 (초) <br /> 기본값 : 0.8 ± 20 % sec |
| similar | 무작위 | 1 : 5 초마다 자동으로 비슷한 모션으로 변경 |
| similarSec | 무작위 | 몇 초마다 자동으로 비슷한 모션으로 변경할지 여부를 지정 |

예 1

메이드 0 dildo_onani_2a01_f를 4 배속 재생, 1 초마다 dildo_onani_2로 시작 모션 (dildo_onani_2b02_f 등)를 4 배속 재생한다.

```
@motion maid = 0 name = dildo_onani_2a01_f similarSec = 1sec speed = 4
```



예 2 :

dildo_onani_1b01_f 재생 시작 후 2 초에 걸쳐 천천히 원래 모션으로 이행한다. 모션이 1 주년 재생 끝날 때까지 처리가 일시 중지되었습니다. 모션 재생이 1 주 후 텍스트를 표시합니다.

```
@motion maid = 0 name = dildo_onani_1b01_f similarSec = 1sec fade = 2sec finish = 1
@show text = 모션 재생 끝
```



예 3

kaiwa_tati_yorokobub_f_once_을 재생하고
완료되면 자동으로 kaiwa_tati_yorokobub_taiki_f을 2 배속으로 재생

```
@motion name = kaiwa_tati_yorokobub_f_once_ afterSpeed ​​= 2 fade = 0.1sec
```

### @talk

지정한 메이드로 음성을 한 번만 재생합니다. 다음 한 번만 재생 음성을 원 음성이라고 부른다.



| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------------------ | ----------------- ------------------------------------------- |
| maid | 무작위 | 동작 대상 메이드을 만든 번호로 지정 |
| name | name 또는 category 필수 | 재생 원 음성 이름을 지정 |
| category | 〃 | 재생 원 음성 카테고리를 지정 |
| finish | 무작위 | 1 : 원 음성의 재생이 끝날 때까지 스크립트를 일시 중지 |
| start | 무작위 | 오디오 파일 재생 시작 위치 (초) <br /> 기본값 : 0 |
| interval | 무작위 | 오디오 파일의 재생 시간 (초)입니다. 0이라면 끝까지 재생. <br /> 기본값 : 0 |
| fadein | 무작위 | 재생 시작 후 서서히 볼륨을 크게 해 나가고 볼륨이 최대가 될 때까지의 시간 (초)입니다. <br /> 기본값 : 0 |



예 1 : 지정된 음성 파일의 0.5 초 위치에서 재생을 시작하여 끝까지 음성을 재생한다. 이야기 시작은 0.2 초에 걸쳐 점점 소리가 커진다. 음성 재생이 끝날 때까지 처리가 일시 중지한다.

```
// 혀, 재미! 빠지면 시간을 잊고 열중 해버립니다.
@talk maid = 0 name = H1_05828 start = 0.5s finish = 1 fade = 0.2s
@show text = 재생이 끝나면이 문자가 표시된다.
```



예 2 : 리소스 테이블 파일 (`oncevoice _ *. csv`이라는 파일)의 "절정 2 '카테고리에서 무작위로 하나를 선택하여 재생한다.

```
@talk maid = 0 category = 절정 2

```



### @talkRepeat

지정한 메이드로 음성을 반복 재생한다. 다음 루프 재생 음성을 루프 음성이라고 부른다.

매개 변수 등은`@ talk`과 같다.



예 1 : 예 2 : 리소스 테이블 파일 (`loopvoice _ *. csv`이라는 파일)의 "절정 후"카테고리에서 무작위로 하나를 선택하여 재생한다.

```
@talkRepeat maid = 0 category = 절정 후

```



### @shapekey

지정한 만든 모양 키 애니메이션을 설정한다. 모양 키 대응 바디에서만 명령이 작동한다.

모양 키 대응 몸 않거나 지정된 모양 키가 존재하지 않는 경우는 아무것도 실행하지 않는다.

| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ----------------------------------- | -------------------------------------------------- ---------- |
| maid | 무작위 | 동작 대상 메이드을 만든 번호로 지정 |
| name | 필수 | 운영되는 모양 키 이름을 지정합니다. |
| mode | 무작위 | 작동 모드를 지정합니다. <br /> 기본값 : fade <br /> <br /> -`fade` : fade 매개 변수에서 지정한 시간에 걸쳐, 모양 키 값을 val 매개 변수에 지정된 값에 변화시킨다. <br /> -`sin` : 모양 키 값을 max에서 min 사이에서 왕복 변화시킨다. 변화 속도는 시간 경과에 Sin 물결 모양으로 변화한다. period 파라미터에 지정된 시간 (초)가 변화주기가된다. <br /> -! [ "sin 파형"이미지 검색 결과 (readme.assets / Wed, 04 Dec 2019 213105.jpeg) <br /> <br /> -`triangle` : 모양 키 값을 max에서 min 사이에서 왕복 변화시킨다. 변화 속도는 시간 경과에 삼각형 모양으로 변화한다. period 파라미터에 지정된 시간 (초)가 변화주기가된다. <br /> -! [관련 이미지] (readme.assets / trianglewave-1575462738614.png) <br /> -`keiren` : 경련하고있는 것처럼 보이게 모양 키 값을 변화시킨다. 구체적으로는 모양 키 값을 max에서 min의 폭의 10 분의 1 배에서 빠르게 변화시키고, 1 번만 max에서 min의 폭과 동일한 변화한다. 이것을 1 세트로, 대개 period 파라미터에서 지정한 주기로 반복한다. 주기는 무작위로 변동한다. <br /> |
| val | fade 모드의 경우 필수 | fade 모드의 경우 모양 키 값을 변화시키는 목표 값을 지정한다. <br /> 그렇지 동작 모드의 경우 무시된다. |
| min | sin, triangle, keiren 모드의 경우 필수 | sin, triangle, keiren 모드의 경우 모양 키 값을 변화시키는 범위의 하한 값을 지정한다. <br /> 그렇지 동작 모드의 경우 무시된다. |
| max | sin, triangle, keiren 모드의 경우 필수 | sin, triangle, keiren 모드의 경우 모양 키 값을 변화시키는 범위의 상한값을 지정한다. <br /> 그렇지 동작 모드의 경우 무시된다. |
| fade | 모든 | fade 모드의 경우 몇 초에 걸쳐 모양 키 값을 목표 값까지 변화시킬 것인지 지정한다. <br /> 그렇지 동작 모드의 경우 무시된다. <br /> 기본값 : 0sec |
| period | sin, triangle, keiren 모드의 경우 필수 | sin, triangle, keiren 모드의 경우 모양 키 값을 변화시키는주기 (초)을 지정한다. <br /> 그렇지 동작 모드의 경우 무시된다. |
| finish | 모든 | sin, triangle, keiren 모드의 경우 몇 초 모양 키의 변화 동작을 지정한다. <br /> 그렇지 동작 모드의 경우 무시된다. <br /> 기본값 : 60sec |



예 1 : 1 번째 만든 'boko "모양 키 (배의 팽창)을 0 초에 걸쳐, 모양 키 값을 0으로 변화시킨다. (모양 키 설정을 재설정하고 싶을 때 라든지 사용)

```
@shapekey maid = 0 name = boko val = 0
```



예 2 : 첫 번째 만든 'boko "모양 키 (배의 팽창)을 0.2 초에 걸쳐, 모양 키 값을 1로 변화시킨다. (지속적으로 모양이 변해가 때 사용)

```
@shapekey maid = 0 name = boko mode = fade val = 1 fade = 0.2
```

예 3 : 1 번째 만든 'orgasm "모양 키를 모양 키 값 0에서 0.2의 폭에서 0.05 초 주기로 Sin 물결로 변화시키고, 5 초 지나면 멈춘다.

```
@shapekey maid = 0 name = orgasm mode = sin min = 0 max = 0.2 period = 0.05s finish = 5s
```



예 4 : 첫 번째 만든 'orgasm "모양 키로 경련 동작시킨다.
```
@shapekey name = orgasm mode = keiren min = 0 max = 0.9 period = 1s
```



### @setProp

지정한 메이드 prop (의류, 액세서리, 도구, 머리 등 옵 젝트)를 장착한다.



| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| maid | 무작위 | 동작 대상 메이드을 만든 번호로 지정 |
| name | 필수 | 장착 prop 파일 이름 |
| part | 무작위 | 장착 할 부위를 명시 적으로 지정. 기본적으로 지정할 필요가 없습니다. <br /> <br /> 지정하지 않으면, prop 파일 이름에서 장착 부위를 자동 판단하기 때문에 잘 prop가 장비 할 수없는 경우에만 사용하는 파라미터. <br /> 가능한 값 : <br /> MuneL, MuneS, MuneTare, RegFat, ArmL, Hara, RegMeet, KubiScl, UdeScl, EyeScl, EyeSclX, EyeSclY, EyePosX, EyePosY, EyeClose, EyeBallPosX, EyeBallPosY, EyeBallSclX, EyeBallSclY , EarNone, EarElf, EarRot, EarScl, NosePos, NoseScl, FaceShape, FaceShapeSlim, MayuShapeIn, MayuShapeOut, MayuX, MayuY, MayuRot, HeadX, HeadY, DouPer, sintyou, koshi, kata west, MuneUpDown, MuneYori, MuneYawaraka, body moza , head, hairf, hairr, hairt, hairs, hairaho, haircolor, skin, acctatoo, accnail, underhair, hokuro, mayu, lip, eye, eye_hi, eye_hi_r, chikubi, chikubicolor, eyewhite, nose, facegloss, wear, skirt, mizugi , bra, panz, stkg, shoes, headset, glove, acchead, accha, acchana, acckamisub, acckami, accmimi, accnip, acckubi, acckubiwa, accheso, accude, accashi, accsenaka, accshippo, accanl, accvag, megane, accxxx, handitem , acchat, onepiece, set_maidwear, set_mywear, set_underwear, set_body, folder_eye, folder_mayu, folder_underhair, folder_skin, folder_eyewhite, kousoku_ upper, kousoku_lower, seieki_naka, seieki_hara, seieki_face, seieki_mune, seieki_hip, seieki_ude, seieki_ashi, <br /> <br /> 기본 :( 지정 없음) |



예 1 : 1 번째 메이드 dress217_glove_i_ (장갑)을 착용한다.
```
@setProp maid = 0 name = dress217_glove_i_
// @ setProp maid = 0 name = dress217_glove_i_ part = glove도 같다.
```



예 2 : 모든 메이드 "_i_mywear017"(토끼 의상 세트)를 장착한다.

```
@setprop name = _i_mywear017
```





### @delProp

지정한 메이드에서 prop (의류, 액세서리, 도구, 머리 등 개체 일반)을 제거한다.

매개 변수는`@ setProp`과 같다.



예 1 : 1 번째 메이드에서 dress217_glove_i_ (장갑)을 제거한다.

```
@delProp maid = 0 name = dress217_glove_i_
// @ delProp maid = 0 name = dress217_glove_i_ part = glove도 같다.
```



### @setSlot

prop (의류, 액세서리, 도구, 머리 등 옵 젝트)를 장착한다. 대상 위치에 prop가 지정되어 이미 제거 된 경우에만 성공적으로 명령 동작한다.



| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| maid | 무작위 | 동작 대상 메이드을 만든 번호로 지정 |
| name | 필수 | 장착하는 슬롯 이름을 지정합니다. <br /> 가능한 값 : <br /> 복합 계 <br /> all : "wear, mizugi, onepiece, bra, skirt, panz, glove, accUde, stkg, shoes, accKubi, accKubiwa"를 동시에 변경 < br /> overwear : "wear, onepiece, skirt, shoes, accKubi, accKubiwa"를 동시에 변경 <br /> exceptacc : "wear, mizugi, onepiece, bra, skirt, panz, stkg"를 동시에 변경 <br /> < br /> 특정 부위에만 <br /> body, head, eye, hairF, hairR, hairS, hairT, wear, skirt, onepiece, mizugi, panz, bra, stkg, shoes, headset, glove, accHead, hairAho, accHana , accHa, accKami_1_, accMiMiR, accKamiSubR, accNipR, HandItemR, accKubi, accKubiwa, accHeso, accUde, accAshi, accSenaka, accShippo, accAnl, accVag, kubiwa, megane, accXXX, chinko, chikubi, accHat, kousoku_upper, kousoku_lower, seieki_naka, seieki_hara , seieki_face, seieki_mune, seieki_hip, seieki_ude, seieki_ashi, accNipL, accMiMiL, accKamiSubL, accKami_2_, accKami_3_, HandItemL, underhair, moza, |




예 1 : 1 번째 메이드 wear를 장착한다.

```
@setSlot maid = 0 name = wear
```


### @delSlot

prop (의류, 액세서리, 도구, 머리 등 개체 일반)을 제거한다. 대상 위치에 prop가 지정되어 있고, 이미 장착 된 경우에만 성공적으로 명령 동작한다.

매개 변수는`@ setSlot`과 같다.



예 1 : 1 번째 메이드에서 wear을 제거한다.

```
@delSlot maid = 0 name = wear
```

예 2 : 모든 제작의 모든 옷 · 액세서리를 제거한다.

```
@delSlot name = all
```



### @setParticle

지정한 만든 particle (몸에서 방출 같은 개체)를 ON으로한다.

| 매개 변수 이름 | 필수? | 사용법 |
| ------------ | ------ | ----------------------------- ------------------------------- |
| maid | 무작위 | 동작 대상 메이드을 만든 번호로 지정 |
| name | 필수 | 재생 particle을 지정합니다. <br /> 가능한 값 : <br /> - toiki1 : 한숨 1 <br /> - toiki2 : 한숨 2 <br /> - aieki1 : 애액 1 <br /> - aieki2 : 애액 2 <br /> - aieki3 : 애액 3 <br /> - nyo : 소변 <br /> - sio : 조수 <br /> |



예 1 : 1 번째 만든 한숨 particle을 ON으로한다.

```
@setParticle maid = 0 name = toiki1

```



### @delParticle

지정한 만든 particle (몸에서 방출 같은 개체)를 OFF로한다.

매개 변수는`@ setParticle`과 같다. 그러나 "name"매개 변수 all을 지정하면 모든 particle을 OFF한다.



예 1 : 1 번째 만든 한숨 particle을 OFF한다.

```
@delParticle maid = 0 name = toiki1

```

예 2 : 모든 제작의 모든 particle을 OFF한다.

```
@delParticle name = all

```





# CSV 파일의 관리 요령

CSV 파일은 엑셀에서 1 시트 1CSV 파일에 대응시켜 일괄 관리하면 좋다. 엑셀의 입력 보조 기능을 활용하여 편하게 작업 할 수 있습니다.



엑셀에서 CSV로 출력 단계

1. (매크로 사용을위한 설정 초기 만 실시) Visual Basic Editor 메뉴에서 [도구] → [참조]를 선택하고 사용 가능한 참조 파일 중에서 "Microsoft ActiveX Data Objects xx Library"에 체크
2. ↓ 매크로를 사용하여 각 시트를 CSV 파일로 출력
3. 출력 한 CSV 파일을 "Sybaris \ UnityInjector \ Config \ Scriplay \ csv"폴더에 배치



엑셀은 기본적으로 Shift-JIS 인코딩으로 파일 출력되지만 Unity는 UTF-8 만 읽을 수 없기 때문에 UTF-8로 출력하고 있습니다.



```vb
Sub writeCSV_UTF8_noBOM_CRLF ()
'
'엑셀 파일과 같은 폴더에 모든 시트를 BOM없이 UTF-8 CSV로 저장
'

Dim ws As Worksheet, savePath As String

'통합 문서를 저장하지 않은 경우 저장
If ActiveWorkbook.Saved = False Then
    If MsgBox ( "북이 아직 저장되지 않습니다. 저장 하시겠습니까?", vbYesNo) = vbNo Then
        Exit Sub
    Else
        ActiveWorkbook.Save
    End If
End If

'전체 시트 저장
Application.DisplayAlerts = False
Application.ScreenUpdating = False
For Each ws In ActiveWorkbook.Worksheets
    ws.Activate
    savePath = ActiveWorkbook.Path & "\"& ws.Name & ".csv"
    'ADODB.Stream 개체를 생성
    Dim adoSt As Object
    Set adoSt = CreateObject ( "ADODB.Stream")

    Dim strLine As String
    Dim i As Long, j As Long
    i = 1
    With adoSt
        .Charset = "UTF-8"
        .LineSeparator = adLF
        .Open

        Do While ws.Cells (i, 1) .Text <> ""
            strLine = ""
            j = 1
            Do While True
                If ws.Cells (i, j + 1) .Text = ""Then Exit Do
                strLine = strLine & ws.Cells (i, j) .Text & ""
                j = j + 1
            Loop
            strLine = strLine & ws.Cells (i, j) .Text & vbCrLf '개행 문자 : \ r \ n
            .WriteText strLine
            i = i + 1
        Loop

        .Position = 0 '스트림의 위치를 ​​0으로한다
        .Type = adTypeBinary '데이터 형식을 바이너리 데이터로 변경
        .Position = 3 '스트림의 위치를 ​​3으로

        Dim byteData () As Byte '임시 저장 용
        byteData = .Read '스트림의 내용을 임시 저장 용 변수에 저장
        .Close '일단 스트림 닫기 (재설정)

        .Open '스트림 열기
        .Write byteData '스트림에 임시 저장 한 데이터를 세척
        .SaveToFile savePath, adSaveCreateOverWrite 'savePath : 전체 경로 adSaveCreateOverWrite : 지정된 파일이 이미 존재하는 경우, 덮어 쓰기
        .Close
    End With

Next

End Sub

```





# 본 MOD를 변경하는 경우에 대해

본 MOD의 수정 · 재배포는 자유이지만, 기존 MOD와의 충돌을 피하기 위해 다음 사항을 지키도록 부탁드립니다.

- MOD 명칭 변경

- 네임 스페이스 변경

  - 소스 코드의 namespace를 다른 이름으로하는

    - ↓의 "Scriplay"다른 이름으로하면된다

    ```c #
    namespace COM3D2.Scriplay.Plugin
    ```

- 별도의 창 ID를 사용할 수

  - 각 창 ID를 다른 숫자 (10 정도 올렸다 숫자)로 변경

  - 예 ( "21"부분)

    ```c #
    GUI.Window (21, node_main, WindowCallback_mainUI, cfg.PluginName + "Main UI"guiTitleStyle);
    ```

# 스크립트 차고
일단 스크립트 차고를 만들었습니다. 자유롭게 이용해주십시오.

https://ux.getuploader.com/Scriplay_Scripts/


# 업데이트 내역

## Scriplay_0.2.0_COM3D2GP-01Ver.1.27.1x64.zip

| 스크립트 버전 | MOD 버전 | 본체 버전 |
| -------------------- | ------------- | -------------- -------------- |
| 2 | 0.2.0 | COM3D2GP-01Ver.1.27.1x64.zip |

- 2019 년 12 월 29 일
- 버전 관리 규칙을 의미 버전 관리로 변경.
- 스크립트 동작이 파괴 변경 있음.
- VR 모양 키 조작 prop 작업 particle 조작 등 기능 대폭 추가.



## COM3D2.Scriplay.Plugin.Ver1.240.1.zip

| 스크립트 버전 | MOD 버전 | 본체 버전 |
| -------------------- | ------------- | -------------- ---------- |
| 1 | 0.2.0 | COM3D2 Ver.1.24.1x64.zip |

- 2019 년 2 월 8 일
- 초판



# 감사

본 MOD는 공개 된 다양한 MOD를 참고로 작성하겠습니다했습니다.

좋은 프로그램을 공개 해 주신 선배에 감사드립니다.



# MOD 사용상의주의

- MOD는 KISS 지원되지 않습니다.
- MOD를 이용하는데있어서 문제가 발생해도 KISS는 일체의 책임을지지 않습니다.
- "사용자 맞춤형 3D2」을 구입하는 분들 만 사용할 수 있습니다.
- "사용자 맞춤형 3D2」에 표시하는 목적 이외의 이용은 금지합니다.
- "맞춤 3D2」에서는 사용하지 마십시오.
- 이러한 사항은 http://kisskiss.tv/kiss/diary.php?no=558을 우선합니다.



# 라이센스

본 MOD는 공중 사용 허가서 (WTFPL)이 적용되어 있습니다.

KISS 약관의 범위 내에서 이용합니다.
