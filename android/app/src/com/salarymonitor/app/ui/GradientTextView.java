package com.salarymonitor.app.ui;

import android.content.Context;
import android.graphics.Canvas;
import android.graphics.LinearGradient;
import android.graphics.Paint;
import android.graphics.Shader;
import android.util.AttributeSet;
import android.widget.TextView;

/** White-to-mint gradient used for the large hero amount. */
public final class GradientTextView extends TextView {
    private static final int[] COLORS = {
            0xFFFFFFFF, 0xFFF0FDFF, 0xFFA5F3FC
    };

    public GradientTextView(Context context) {
        super(context);
    }

    public GradientTextView(Context context, AttributeSet attrs) {
        super(context, attrs);
    }

    public GradientTextView(Context context, AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
    }

    private void applyShader(int width) {
        if (width <= 0) {
            return;
        }
        Paint paint = getPaint();
        paint.setShader(new LinearGradient(0f, 0f, width, 0f,
                COLORS, null, Shader.TileMode.CLAMP));
    }

    @Override
    protected void onSizeChanged(int w, int h, int oldw, int oldh) {
        super.onSizeChanged(w, h, oldw, oldh);
        applyShader(w);
    }

    @Override
    protected void onDraw(Canvas canvas) {
        if (getPaint().getShader() == null) {
            applyShader(getWidth());
        }
        super.onDraw(canvas);
    }
}
