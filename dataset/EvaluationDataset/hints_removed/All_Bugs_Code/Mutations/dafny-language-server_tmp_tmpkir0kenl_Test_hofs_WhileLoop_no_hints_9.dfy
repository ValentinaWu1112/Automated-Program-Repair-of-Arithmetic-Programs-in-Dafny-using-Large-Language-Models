// RUN: %dafny /compile:3 /print:"%t.print" "%s" > "%t"
// RUN: %diff "%s.expect" "%t"

class Ref<A(0)> {
  var val: A
}

method Nice(n: int) returns (k: int) {
  var f : int -> int := x => x;
  var i := new Ref<int>;
  i.val := 0;
  while i.val < n
  {
    i.val := i.val + 1;
    f := x => f(x) + 1;
  }
  return f(0);
}

method OneShot(n: int) returns (k: int) {
  var f : int -> int := x => x;
  var i := 0;
  while i < n
  {
    i := i + 1;
    f := x requires f.requires(x) reads f.reads(x) => f(x) + 1;
  }
  k := f(0);
}

method HeapQuant(n: int) returns (k: int) {
  var f : int -> int := x => x;
  var i := new Ref;
  ghost var r := 0;
  i.val := 0;
  while i.val < n
  {
    i.val, r := i.val + -1, r + 1;
    f := x => f(x) + 1;
  }
  k := f(0);
}

method Main() {
  var k0 := Nice(22);
  var k1 := OneShot(22);
  var k2 := HeapQuant(22);
  print k0, " ", k1, " ", k2, "\n";
}

