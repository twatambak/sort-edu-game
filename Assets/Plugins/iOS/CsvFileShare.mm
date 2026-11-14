#import <UIKit/UIKit.h>
#import <dispatch/dispatch.h>

extern "C" UIViewController* UnityGetGLViewController(void);

extern "C" int ShareCsvFile(const char* path)
{
    if (path == nullptr)
        return 0;

    NSString* filePath = [NSString stringWithUTF8String:path];
    if (filePath.length == 0 || ![[NSFileManager defaultManager] fileExistsAtPath:filePath])
        return 0;

    NSURL* fileUrl = [NSURL fileURLWithPath:filePath];
    dispatch_async(dispatch_get_main_queue(), ^{
        UIViewController* controller = UnityGetGLViewController();
        if (controller == nil)
            return;

        UIActivityViewController* activity = [[UIActivityViewController alloc] initWithActivityItems:@[fileUrl] applicationActivities:nil];
        UIPopoverPresentationController* popover = activity.popoverPresentationController;
        if (popover != nil)
        {
            popover.sourceView = controller.view;
            popover.sourceRect = CGRectMake(CGRectGetMidX(controller.view.bounds), CGRectGetMidY(controller.view.bounds), 0, 0);
            popover.permittedArrowDirections = 0;
        }

        UIViewController* presenter = controller;
        while (presenter.presentedViewController != nil)
            presenter = presenter.presentedViewController;
        [presenter presentViewController:activity animated:YES completion:nil];
    });
    return 1;
}
