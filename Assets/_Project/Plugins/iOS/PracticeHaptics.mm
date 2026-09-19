#import <UIKit/UIKit.h>
#import <dispatch/dispatch.h>
#include <stdint.h>

// UIKit owns support detection. Unsupported hardware quietly supplies no feedback.
// All generator access is on the main queue; no Core Haptics engine or audio session is changed.
static UIImpactFeedbackGenerator *brLight;
static UIImpactFeedbackGenerator *brContact;
static bool brEnabled = false;

static void BRInitializeOnMain(void *)
{
    if (!brLight) brLight = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
    if (!brContact) brContact = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
}
static void BREnabledOnMain(void *value) { brEnabled = (intptr_t)value != 0; }
static void BRPrepareOnMain(void *)
{
    if (!brEnabled) return;
    [brLight prepare]; [brContact prepare];
}
static void BRPulseOnMain(void *value)
{
    if (!brEnabled || UIApplication.sharedApplication.applicationState != UIApplicationStateActive) return;
    if ((intptr_t)value != 0) [brContact impactOccurred];
    else [brLight impactOccurred];
}
static void BRDisposeOnMain(void *)
{
    brEnabled = false;
#if !__has_feature(objc_arc)
    [brLight release]; [brContact release];
#endif
    brLight = nil; brContact = nil;
}
static void BROnMain(dispatch_function_t function, void *context)
{
    if (NSThread.isMainThread) function(context);
    else dispatch_async_f(dispatch_get_main_queue(), context, function);
}
extern "C" void BRFeedbackInitialize() { BROnMain(BRInitializeOnMain, nullptr); }
extern "C" void BRFeedbackEnabled(int enabled) { BROnMain(BREnabledOnMain, (void *)(intptr_t)enabled); }
extern "C" void BRFeedbackPrepare() { BROnMain(BRPrepareOnMain, nullptr); }
extern "C" void BRFeedbackPulse(int contact) { BROnMain(BRPulseOnMain, (void *)(intptr_t)contact); }
extern "C" void BRFeedbackDispose() { BROnMain(BRDisposeOnMain, nullptr); }
