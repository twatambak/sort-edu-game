package com.ondevai.csvshare;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.ClipData;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.util.Log;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.util.List;

public final class CsvShare {
    private static final String TAG = "OndeVaiCsvShare";
    private static final String FILE_NAME = "export.csv";

    private CsvShare() {
    }

    public static boolean shareCsv(Activity activity, String sourcePath, String chooserTitle) {
        if (activity == null || sourcePath == null) {
            return false;
        }

        File source = new File(sourcePath);
        File exportDirectory = new File(activity.getCacheDir(), "csv-export");
        if (!source.isFile() || !source.canRead() ||
                (!exportDirectory.isDirectory() && !exportDirectory.mkdirs())) {
            return false;
        }

        File sharedFile = new File(exportDirectory, FILE_NAME);
        try (InputStream input = new FileInputStream(source);
             OutputStream output = new FileOutputStream(sharedFile, false)) {
            byte[] buffer = new byte[8192];
            int count;
            while ((count = input.read(buffer)) != -1) {
                output.write(buffer, 0, count);
            }
        } catch (IOException exception) {
            Log.e(TAG, "Could not prepare the CSV for sharing.", exception);
            return false;
        }

        Uri fileUri = new Uri.Builder()
                .scheme("content")
                .authority(activity.getPackageName() + ".csv-export")
                .appendPath(FILE_NAME)
                .build();
        Intent sendIntent = new Intent(Intent.ACTION_SEND);
        sendIntent.setType("text/csv");
        sendIntent.putExtra(Intent.EXTRA_STREAM, fileUri);
        sendIntent.setClipData(ClipData.newUri(activity.getContentResolver(), FILE_NAME, fileUri));
        sendIntent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);

        PackageManager packageManager = activity.getPackageManager();
        List<?> targets = packageManager.queryIntentActivities(sendIntent, 0);
        if (targets == null || targets.isEmpty()) {
            return false;
        }

        Intent chooser = Intent.createChooser(sendIntent, chooserTitle);
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    activity.startActivity(chooser);
                } catch (ActivityNotFoundException exception) {
                    Log.e(TAG, "No app could open the CSV share sheet.", exception);
                }
            }
        });
        return true;
    }
}
