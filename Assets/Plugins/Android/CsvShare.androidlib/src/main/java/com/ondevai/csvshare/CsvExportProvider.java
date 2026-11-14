package com.ondevai.csvshare;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.database.Cursor;
import android.database.MatrixCursor;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import android.provider.OpenableColumns;

import java.io.File;
import java.io.FileNotFoundException;
public final class CsvExportProvider extends ContentProvider {
    private static final String FILE_NAME = "export.csv";

    @Override
    public boolean onCreate() {
        return true;
    }

    @Override
    public String getType(Uri uri) {
        return getExportFile(uri) == null ? null : "text/csv";
    }

    @Override
    public Cursor query(Uri uri, String[] projection, String selection, String[] selectionArgs, String sortOrder) {
        File file = getExportFile(uri);
        if (file == null || !file.isFile()) {
            return null;
        }

        String[] columns = projection != null
                ? projection
                : new String[]{OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE};
        MatrixCursor cursor = new MatrixCursor(columns);
        Object[] values = new Object[columns.length];
        for (int i = 0; i < columns.length; i++) {
            if (OpenableColumns.DISPLAY_NAME.equals(columns[i])) {
                values[i] = FILE_NAME;
            } else if (OpenableColumns.SIZE.equals(columns[i])) {
                values[i] = file.length();
            }
        }
        cursor.addRow(values);
        return cursor;
    }

    @Override
    public ParcelFileDescriptor openFile(Uri uri, String mode) throws FileNotFoundException {
        File file = getExportFile(uri);
        if (!"r".equals(mode) || file == null || !file.isFile()) {
            throw new FileNotFoundException("CSV export not found.");
        }

        return ParcelFileDescriptor.open(file, ParcelFileDescriptor.MODE_READ_ONLY);
    }

    @Override
    public Uri insert(Uri uri, ContentValues values) {
        throw new UnsupportedOperationException("CSV export is read-only.");
    }

    @Override
    public int delete(Uri uri, String selection, String[] selectionArgs) {
        throw new UnsupportedOperationException("CSV export is read-only.");
    }

    @Override
    public int update(Uri uri, ContentValues values, String selection, String[] selectionArgs) {
        throw new UnsupportedOperationException("CSV export is read-only.");
    }

    private File getExportFile(Uri uri) {
        if (uri == null || uri.getPathSegments().size() != 1 ||
                !FILE_NAME.equals(uri.getLastPathSegment()) || getContext() == null) {
            return null;
        }

        return new File(new File(getContext().getCacheDir(), "csv-export"), FILE_NAME);
    }
}
