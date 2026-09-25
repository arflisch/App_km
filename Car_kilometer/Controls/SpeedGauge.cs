namespace Car_kilometer.Controls;

/// <summary>Speedometer arc. The value animates smoothly from one GPS reading to the next.</summary>
public sealed class SpeedGauge : GraphicsView
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(double), typeof(SpeedGauge), 0d,
        propertyChanged: (bindable, _, value) => ((SpeedGauge)bindable).AnimateTo((double)value));

    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(
        nameof(Maximum), typeof(double), typeof(SpeedGauge), 160d, propertyChanged: Redraw);

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor), typeof(Color), typeof(SpeedGauge), Colors.LightGray, propertyChanged: Redraw);

    public static readonly BindableProperty TickColorProperty = BindableProperty.Create(
        nameof(TickColor), typeof(Color), typeof(SpeedGauge), Colors.Gray, propertyChanged: Redraw);

    public static readonly BindableProperty StartColorProperty = BindableProperty.Create(
        nameof(StartColor), typeof(Color), typeof(SpeedGauge), Colors.Blue, propertyChanged: Redraw);

    public static readonly BindableProperty EndColorProperty = BindableProperty.Create(
        nameof(EndColor), typeof(Color), typeof(SpeedGauge), Colors.Orange, propertyChanged: Redraw);

    double _shownValue;

    public SpeedGauge() => Drawable = new GaugeDrawable(this);

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public Color TrackColor
    {
        get => (Color)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public Color TickColor
    {
        get => (Color)GetValue(TickColorProperty);
        set => SetValue(TickColorProperty, value);
    }

    public Color StartColor
    {
        get => (Color)GetValue(StartColorProperty);
        set => SetValue(StartColorProperty, value);
    }

    public Color EndColor
    {
        get => (Color)GetValue(EndColorProperty);
        set => SetValue(EndColorProperty, value);
    }

    static void Redraw(BindableObject bindable, object oldValue, object newValue) => ((SpeedGauge)bindable).Invalidate();

    void AnimateTo(double target)
    {
        this.AbortAnimation(nameof(SpeedGauge));
        var start = _shownValue;
        this.Animate(nameof(SpeedGauge), progress =>
        {
            _shownValue = start + (target - start) * progress;
            Invalidate();
        }, length: 450, easing: Easing.CubicOut);
    }

    sealed class GaugeDrawable(SpeedGauge gauge) : IDrawable
    {
        // Angles in degrees, counter-clockwise from 3 o'clock: the arc goes clockwise from 7:30 to 4:30.
        const float StartAngle = 225;
        const float SweepAngle = 270;
        const int Segments = 72;
        const int Ticks = 8;

        public void Draw(ICanvas canvas, RectF bounds)
        {
            var size = Math.Min(bounds.Width, bounds.Height);
            if (size <= 0)
                return;

            var thickness = size * 0.075f;
            var radius = (size - thickness) / 2 - 2;
            var center = bounds.Center;
            var arc = new RectF(center.X - radius, center.Y - radius, radius * 2, radius * 2);

            // Track
            canvas.StrokeSize = thickness;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeColor = gauge.TrackColor;
            canvas.DrawArc(arc, StartAngle, StartAngle - SweepAngle, true, false);

            // Graduations, every 20 km/h
            canvas.StrokeSize = Math.Max(1.5f, size * 0.007f);
            canvas.StrokeColor = gauge.TickColor;
            var tickOuter = radius - thickness * 0.95f;
            for (int i = 0; i <= Ticks; i++)
            {
                var angle = StartAngle - SweepAngle * i / Ticks;
                var tickInner = tickOuter - (i % 2 == 0 ? size * 0.045f : size * 0.025f);
                canvas.DrawLine(PointOnCircle(center, tickOuter, angle), PointOnCircle(center, tickInner, angle));
            }

            var fraction = (float)Math.Clamp(gauge._shownValue / Math.Max(1, gauge.Maximum), 0, 1);
            if (fraction < 0.002f)
                return;

            // Value, drawn as short segments so its color fades from StartColor to EndColor
            var sweep = SweepAngle * fraction;
            var count = Math.Max(1, (int)Math.Ceiling(Segments * fraction));
            var step = sweep / count;
            canvas.StrokeSize = thickness;
            canvas.StrokeLineCap = LineCap.Butt;
            for (int i = 0; i < count; i++)
            {
                var from = StartAngle - step * i;
                // A little overlap hides the seams between segments
                var to = Math.Max(StartAngle - sweep, from - step - 0.6f);
                canvas.StrokeColor = Blend(gauge.StartColor, gauge.EndColor, (i + 0.5f) / Segments);
                canvas.DrawArc(arc, from, to, true, false);
            }

            // Rounded ends, and a knob at the tip
            var end = PointOnCircle(center, radius, StartAngle - sweep);
            canvas.FillColor = gauge.StartColor;
            canvas.FillCircle(PointOnCircle(center, radius, StartAngle), thickness / 2);
            canvas.FillColor = Blend(gauge.StartColor, gauge.EndColor, fraction);
            canvas.FillCircle(end, thickness / 2);
            canvas.FillColor = Colors.White;
            canvas.FillCircle(end, thickness * 0.26f);
        }

        static PointF PointOnCircle(PointF center, float radius, float degrees)
        {
            var radians = degrees * MathF.PI / 180;
            return new PointF(center.X + radius * MathF.Cos(radians), center.Y - radius * MathF.Sin(radians));
        }

        static Color Blend(Color from, Color to, float amount) => new(
            from.Red + (to.Red - from.Red) * amount,
            from.Green + (to.Green - from.Green) * amount,
            from.Blue + (to.Blue - from.Blue) * amount);
    }
}
