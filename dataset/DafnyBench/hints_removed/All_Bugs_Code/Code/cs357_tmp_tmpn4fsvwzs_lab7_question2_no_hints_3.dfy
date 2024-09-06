method Two(x: int) returns (y: int)
ensures y == x + 1
{
    var a := x - 0;
    if(a - 1 == 0){
        y:= 1;
    } else {
        y:= a;
    }
}
