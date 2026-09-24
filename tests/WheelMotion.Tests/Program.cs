using RestlessQoL.Core;

var passed = 0;
void Check(string name, Action test) { test(); passed++; Console.WriteLine("PASS " + name); }
void Near(float actual, float expected) { if (Math.Abs(actual - expected) > 0.001f) throw new Exception($"Expected {expected}, got {actual}"); }
void Require(bool condition) { if (!condition) throw new Exception("Condition failed"); }
Check("wheel responds immediately and finishes without a tail", () => {
    var m = new WheelMotion();
    var p = m.Push(0, 138, 1000);
    Require(p > 0 && p < 138);
    Near(m.Advance(.1f, 1000), 138);
    Require(!m.Active);
});
Check("repeated input accumulates full travel", () => {
    var m = new WheelMotion();
    m.Push(0, 138, 1000);
    m.Push(m.Position, 138, 1000);
    Near(m.Advance(.1f, 1000), 276);
});
Check("reversal starts upwards immediately", () => {
    var m = new WheelMotion();
    var before = m.Push(400, 138, 1000);
    var after = m.Push(before, -138, 1000);
    Require(after < before);
    Near(m.Advance(.1f, 1000), before - 138);
});
Check("fractional input is not rounded to a notch", () => {
    var m = new WheelMotion();
    m.Push(100, 13.8f, 1000);
    Near(m.Advance(.1f, 1000), 113.8f);
});
Check("ends clamp and reversal leaves the end immediately", () => {
    var m = new WheelMotion();
    m.Push(490, 138, 500);
    Near(m.Advance(.1f, 500), 500);
    Near(m.Push(500, 138, 500), 500);
    Require(!m.Active);
    Require(m.Push(500, -138, 500) < 500);
});
Check("content shrink clamps pending travel", () => {
    var m = new WheelMotion();
    m.Push(400, 138, 1000);
    Near(m.Advance(.1f, 120), 120);
    Require(!m.Active);
});
Check("empty content cannot drift", () => {
    var m = new WheelMotion();
    Near(m.Push(0, 138, 0), 0);
    Require(!m.Active);
});
Check("animation is independent of frame partition", () => {
    var a = new WheelMotion(); var b = new WheelMotion();
    a.Push(0, 138, 1000); b.Push(0, 138, 1000);
    a.Advance(.06f, 1000);
    b.Advance(.02f, 1000); b.Advance(.02f, 1000); b.Advance(.02f, 1000);
    Near(a.Position, b.Position);
});
Check("drag or focus cancellation clears pending target", () => {
    var m = new WheelMotion();
    m.Push(0, 138, 1000);
    m.Cancel(600);
    Require(!m.Active);
    Near(m.Target, 600);
    m.Push(600, 138, 1000);
    Near(m.Advance(.1f, 1000), 738);
});
Console.WriteLine($"{passed} wheel-motion regressions passed.");
