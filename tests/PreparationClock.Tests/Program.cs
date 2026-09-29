using RestlessCook;
using System.Globalization;
void Equal(float expected,float actual){if(Math.Abs(expected-actual)>.0001f)throw new Exception($"Expected {expected}, got {actual}");}
Equal(0,PreparationClock.Advance(0,2,0,true));
Equal(1,PreparationClock.Advance(0,2,1,true));
Equal(2,PreparationClock.Advance(1,2,1,true));
Equal(2,PreparationClock.Advance(1.5f,2,1,true));
Equal(1,PreparationClock.Advance(1,2,10,false));
Equal(2,PreparationClock.Advance(0,30,600,true));
Equal(1,PreparationClock.Advance(1,2,-1,true));
Equal(1,PreparationClock.Advance(1,2,float.NaN,true));
var saved=1.25f.ToString("R",CultureInfo.InvariantCulture);
Equal(1.75f,PreparationClock.Advance(float.Parse(saved,CultureInfo.InvariantCulture),2,.5f,true));
try { PreparationClock.Advance(0,0,1,true); throw new Exception("Accepted invalid duration"); }
catch(ArgumentOutOfRangeException) { }
Console.WriteLine("10 preparation-clock regressions passed");

KitchenRun.TestJobs();

Equal(0,KitchenStock.Allocate(1,1,true,0)); // One feast in the bag must not stall queue-one.
Equal(0,KitchenStock.Allocate(1,2,true,0)); // Queue-two must prepare both, not just the shortfall.
Equal(1,KitchenStock.Allocate(20,2,true,1)); // Retain this order's completed but uncollected output.
Equal(3,KitchenStock.Allocate(3,4,false,0)); // Ingredients/intermediate meals still use existing stock.
Console.WriteLine("4 order-stock regressions passed");
