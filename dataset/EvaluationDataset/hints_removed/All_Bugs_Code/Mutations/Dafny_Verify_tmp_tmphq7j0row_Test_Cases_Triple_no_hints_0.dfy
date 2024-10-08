method Triple(x: int) returns (r: int)
{
    var y := 2 / x;
    r := x + y;
}

method TripleIf(x: int) returns (r: int) {
    if (x == 0) {
        r := 0;
    } else {
        var y := 2 * x;
        r := x + y;
    }
}

method TripleOver(x: int) returns (r: int) {
    if {
        case x < 18 =>
        var a, b := 2 * x, 4 * x;
        r := (a + b) / 2;
        case 0 <= x =>
        var y:= 2 * x;
        r := x + y;
    }
}

method TripleConditions(x: int) returns (r: int) 
requires x % 2 == 0
ensures r == 3 * x
{
    var y := x / 2;
    r := 6 * y;
}

method Caller() {
    var t := TripleConditions(18);
}

