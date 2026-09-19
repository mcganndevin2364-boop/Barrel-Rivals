package com.barrelrivals.practice;

import android.app.Activity;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.view.HapticFeedbackConstants;
import android.view.View;
import com.unity3d.player.UnityPlayer;

/** System feedback honors device/view settings. No vibration permission or long generic vibration. */
public final class PracticeHaptics {
    private static final Handler MAIN = new Handler(Looper.getMainLooper());
    private static volatile boolean enabled;
    private static final Runnable LIGHT = new Runnable() {
        @Override public void run() { perform(false); }
    };
    private static final Runnable CONTACT = new Runnable() {
        @Override public void run() { perform(true); }
    };
    private PracticeHaptics() { }
    public static void initialize() { enabled = false; }
    public static void setEnabled(int value) {
        enabled = value != 0;
        if (!enabled) { MAIN.removeCallbacks(LIGHT); MAIN.removeCallbacks(CONTACT); }
    }
    public static void pulse(int contact) {
        if (!enabled) return;
        Runnable pulse = contact == 0 ? LIGHT : CONTACT;
        MAIN.removeCallbacks(pulse);
        MAIN.post(pulse);
    }
    private static void perform(boolean contact) {
        Activity activity = UnityPlayer.currentActivity;
        if (!enabled || activity == null || activity.isFinishing()) return;
        View view = activity.getWindow().getDecorView();
        if (!view.hasWindowFocus()) return;
        int effect = contact ? HapticFeedbackConstants.CONTEXT_CLICK :
            Build.VERSION.SDK_INT >= 30 ? HapticFeedbackConstants.CONFIRM : HapticFeedbackConstants.VIRTUAL_KEY;
        view.performHapticFeedback(effect); // No IGNORE_GLOBAL_SETTING / IGNORE_VIEW_SETTING flags.
    }
}
