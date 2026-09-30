#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <GoogleMobileAds/GoogleMobileAds.h>

// Khai báo hàm lấy ViewController gốc của Unity để hiển thị UI Ad lên trên
#ifdef __cplusplus
extern "C" {
#endif
    UIViewController* UnityGetGLViewController(void);
#ifdef __cplusplus
}
#endif

// Định nghĩa các loại Ad tương thích với file Java
typedef NS_ENUM(NSInteger, FsnAdType) {
    FsnAdTypeNormal = 0,
    FsnAdTypeFakeClose = 1,
    FsnAdTypeCountdown = 2,
    FsnAdTypeCollapsible = 3
};

// Định nghĩa các Style Layout tương thích với XML
typedef NS_ENUM(NSInteger, FsnLayoutType) {
    FsnLayoutTypeStyle1 = 0,
    FsnLayoutTypeStyle2 = 1,
    FsnLayoutTypeStyle3 = 2,
    FsnLayoutTypeStyle4 = 3
};

// Lớp lưu trữ trạng thái của từng lượt quảng cáo.
// Lưu ý:
// Trước đây state được key theo type 0/1/2/3.
// Nhưng nhiều placement khác nhau có thể cùng dùng type Native = 0,
// nên iOS callback dễ bị ghi đè/route sai.
// Bản này key theo requestId riêng của từng FsnAd instance.
@interface FsnAdStateIOS : NSObject

@property (nonatomic, assign) int requestId;
@property (nonatomic, assign) int nativeType;

@property (nonatomic, strong) NSString *adUnitId;
@property (nonatomic, assign) NSInteger countDownSec;
@property (nonatomic, assign) long long delayForCountDown;
@property (nonatomic, assign) GADMediaAspectRatio mediaAspectRatio;

@property (nonatomic, strong) GADNativeAd *nativeAd;
@property (nonatomic, assign) BOOL shouldCloseAfterAdClick;

// Bắt buộc giữ strong reference tới GADAdLoader.
// Nếu không, loader có thể bị release sau khi loadRequest,
// làm didReceiveNativeAd / didFailToReceiveAdWithError không bắn về.
@property (nonatomic, strong) GADAdLoader *adLoader;

@property (nonatomic, assign) BOOL isLoading;
@property (nonatomic, assign) NSTimeInterval lastLoadTime;
@property (nonatomic, strong) UIView *adContainerView;
@property (nonatomic, strong) NSTimer *countDownTimer;
@property (nonatomic, assign) NSInteger currentRemainingSec;

@end

// Lớp quản lý chính xử lý AdMob SDK
@interface FsnAdIOS : NSObject <GADNativeAdLoaderDelegate, GADNativeAdDelegate>

+ (instancetype)sharedInstance;

- (void)initNativeWithRequestId:(int)requestId
                           type:(int)type
                     mediaRatio:(int)ratio
                       adUnitId:(NSString*)adUnitId
                   countDownSec:(int)countDownSec
              delayForCountDown:(long)delay;

- (void)loadNativeAdWithRequestId:(int)requestId
                       mediaRatio:(int)ratio;

- (BOOL)isNativeAdReadyWithRequestId:(int)requestId;

- (void)showNativeAdWithRequestId:(int)requestId
                       layoutType:(int)layoutType
              overlayOpenStorePos:(int)openStorePos
                  overlayClosePos:(int)closePos;

- (void)hideNativeAdWithRequestId:(int)requestId;

@end