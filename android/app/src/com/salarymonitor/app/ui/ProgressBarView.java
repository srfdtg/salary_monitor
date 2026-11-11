package com.salarymonitor.app.ui;

import android.content.Context;
import android.graphics.Canvas;
import android.graphics.LinearGradient;
import android.graphics.Paint;
import android.graphics.RectF;
import android.graphics.Shader;
import android.util.AttributeSet;
import android.view.View;

/** Rounded, gradient-filled progress bar with no platform theme dependencies. */
public final class ProgressBarView extends View {
    private final Paint trackPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final Paint fillPaint = new Paint(Paint.ANTI_ALIAS_FLAG);
    private final RectF rect = new RectF();
    private double progress;

    public ProgressBarView(Context context) {
        super(context);
        init();
    }

    public ProgressBarView(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    public ProgressBarView(Context context, AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
        init();
    }

    private void init() {
        trackPaint.setColor(0x16FFFFFF);
        fillPaint.setColor(0xFF2DD4BF);
    }

    public void setProgress(double value) {
        progress = Math.max(0.0, Math.min(1.0, value));
        invalidate();
    }

    @Override
    protected void onDraw(Canvas canvas) {
        super.onDraw(canvas);
        int width = getWidth();
        int height = getHeight();
        if (width <= 0 || height <= 0) {
            return;
        }
        float radius = height / 2f;
        rect.set(0f, 0f, width, height);
        canvas.drawRoundRect(rect, radius, radius, trackPaint);

        float fillWidth = (float) (width * progress);
        if (fillWidth <= 0f) {
            return;
        }
        if (fillWidth < height) {
            fillWidth = height;
        }
        if (fillPaint.getShader() == null) {
            fillPaint.setShader(new LinearGradient(0f, 0f, width, 0f,
                    new int[]{0xFF22D3EE, 0xFF2DD4BF, 0xFF818CF8},
                    null, Shader.TileMode.CLAMP));
        }
        rect.set(0f, 0f, fillWidth, height);
        canvas.drawRoundRect(rect, radius, radius, fillPaint);
    }
}
