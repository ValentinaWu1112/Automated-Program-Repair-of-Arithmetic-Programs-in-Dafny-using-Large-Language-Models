method main() returns (t1: int, t2: int, x: int, y: int)
ensures y >= 1
{
    x := 1;
    y := 1;
    t1 := 0;
    t2 := 0;

    while(x <= 100000) 
    {
        t1 := x;
        t2 := y;
        x := t2 - t1;//buggy line

        y := t1 + t2;
    }
}

