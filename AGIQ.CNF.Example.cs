using AGIQ.CNF;

var model = new CnfModel();

var x1 = model.Var("x1");
var x2 = model.Var("x2");
var x3 = model.Var("x3");
var x4 = model.Var("x4");

var workers = model.Vars("worker", 5);

model.AddOr(x1, x2);
model.AddImplication(x3, x4);
model.AddExactlyOne(workers);

model.Save("task.cnf");
