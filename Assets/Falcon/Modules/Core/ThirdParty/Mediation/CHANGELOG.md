### CHANGELOG
# 1.4.23
- sửa logic bị where = null

# 1.4.21, 1.4.22
- thêm logic anti fraude user (MO)

# 1.4.20
- thêm algorithm 7 cho multicall

# 1.4.19
- sửa luồng CSAdRequestLog estimate ecpm = 0

# 1.4.18
- sửa CSAdFinish, CSAdInfo, CSAdStart, CSAdRequestLog vào queue 

# 1.4.17
- thêm algorithm 6 cho multicall

# 1.4.16
- thêm template cho FSN
- thêm callback onFail khi gọi hàm showInter, showReward sớm.

# 1.4.15
- fix lỗi FSN policy

# 1.4.14
- fix lỗi build android
- cập nhật lại luồng cho FSN ios

# 1.4.13
- fix lỗi khi build Ios

# 1.4.12
- thêm init FSN trong manual

# 1.4.11
- cập nhật lại luồng manual init, có thể gọi bất cứ lúc nào

# 1.4.10
- sửa lại luồng cho collapsible fsn

# 1.4.8, 1.4.9
- fix lỗi fsn IOS

# 1.4.7
- lưu cache low end device

# 1.4.6
- lưu cache campaignId cho appsflyer

# 1.4.5
- làm lại luồng cho fsn
- hỗ trợ filter theo campaignId lấy từ appsflyer
- sử dụng remote config từ d4g cho fsn
- thêm các template cho fsn

# 1.4.4
- fix lỗi ko hiển thị được interstitial trên editor

# 1.4.3
- thêm param level cho BannerLogService

# 1.4.2
- fix lỗi load interstitial fsn

# 1.4.1
- thêm dependencies cho fsn
- fix lỗi build cho fsn
- chuyển từ remote config từ data4game sang cms

# 1.4.0
- thêm fsn

# 1.3.38
- thêm logic load ads

# 1.3.37
- thêm check null fix lỗi show inter

# 1.3.36
- thêm logic load ads mới (4 và 5)

# 1.3.35
- thêm hàm get banner height

# 1.3.34
- bỏ multicall trong setting local

# 1.3.33
- thêm logic load ads mới

# 1.3.32
- thêm event bus lắng nghe remove ads

# 1.3.31
- fix lỗi scripting define

# 1.3.30
- update remote config mới cms

# 1.3.28, 1.3.29
- fix log bamboo/taichi

# 1.3.25, 1.3.26, 1.3.27
- fix log bamboo 3 cho iron source

# 1.3.24
- update logic bamboo 3 dùng thêm cho appopen, rewarded, interstitial

# 1.3.23
- update logic bamboo 3

# 1.3.22
- thêm Dontdestroy cho LogInstance

# 1.3.21
- fix bug check null ad request

# 1.3.20
- update logic bamboo 2

# 1.3.19
- update logic taichi/bamboo
- fix minor bug

# 1.3.18
- fix log placement banner = null

# 1.3.17
- fix log placement id = null

# 1.3.16
- fix log placement id = null

# 1.3.15
- thêm chức năng manual Init
- hỗ trợ app open monetize

# 1.3.14
- fix lỗi script define

# 1.3.13
- thêm action bannerLoaded
- thêm hàm check IsBannerReady

# 1.3.12
- fix lỗi khi build Ios

# 1.3.10, 1.3.11
- fix lỗi khi build exe

# 1.3.9
- cập nhật log ltv

# 1.3.8
- cập nhật log ltv v2

# 1.3.7
- fix lỗi log ltv

# 1.3.6
- cập nhật logic taichi/bamboo

# 1.3.5
- thêm param level khi log mmp

# 1.3.4
- bỏ gma khỏi setting

# 1.3.3
- bỏ script define gma

# 1.3.2
- thêm config cho giới hạn level thấp nhất có thể show AOA

# 1.3.1
- cập nhật document.

# 1.3.0
- Hỗ trợ multi call cho ironsource, song song với Max
- tối ưu giảm crash/anr cho máy yếu
- thêm log cho bamboo/taichi (phục vụ cho segment user AI)
- thêm log CSAdInfo

# 1.2.41
- fix lỗi banner tự show sau khi hide

# 1.2.40
- fix lỗi log ads display với multicall

# 1.2.39
- thêm button close trên banner

# 1.2.38
- thêm hàm log display và displayFailed

# 1.2.37
- fix lỗi khi log ads lên bigdata

# 1.2.36
- fix lỗi khi log ads lên bigdata

# 1.2.35
- fix lỗi khi log ads lên bigdata

# 1.2.34
- Thêm Cooldown time cho AppOpenAds
- Thêm hàm InogreNextTimeAoa
- sửa log lên GameData4AdInfo

# 1.2.33
- sửa lỗi class CSAdStart và CSAdFinish

# 1.2.32
- fix lỗi delay multi call.

# 1.2.31
- fix lỗi null khi gọi quá sớm.

# 1.2.30
- thêm log ads
- update ironsource (levelplay) lên 9.0.0

# 1.2.29
- fix symbol script

# 1.2.28
- thêm callback banner show/hide
- fix lỗi show inter thay vì show rewarded

# 1.2.27
- fix lỗi symbol script

# 1.2.26
- fix lỗi thời gian finish ads
- thêm FActionLog khi start và finish ads

# 1.2.25
- fix lỗi thừa param gma service

# 1.2.24
- thêm log ltv

# 1.2.23
- fix lỗi revenue = -1 của max

# 1.2.22
- thêm scripting define hỗ trợ window (build map editor)
- thêm hàm log khi bắt đầu và sau khi xem xong ads

# 1.2.21
- tách module add-on mediation MO từ module chính mediation

# 1.2.20
- fix lỗi build exe

# 1.2.19
- chỉnh lại thứ tự param khi gọi show quảng cáo, ưu tiên placementId phải khác rỗng, để so sánh với trên remote config
- thêm param placementId cho banner

# 1.2.18
- chỉnh sửa hàm lấy trạng thái của quảng cáo tại từng vị trí

# 1.2.17
- thêm hàm lấy trạng thái của quảng cáo tại từng vị trí

# 1.2.15
- thêm param thứ 4, là thời gian delay của từng id quảng cáo

# 1.2.12
- fix minor bug

# 1.2.11
- Cập nhật logic tối ưu load quảng cáo mới
