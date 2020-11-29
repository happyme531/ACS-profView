local profview = GameMain:NewMod("profview");
profview.dll=CS.System.Reflection.Assembly.LoadFrom(CS.ModsMgr.Instance:FindMod("profview").Path.."\\Scripts\\bin\\Debug\\net4.0\\profview.dll");

function profview:OnInit()
    local profview_main = CS.System.Activator.CreateInstance(profview.dll:GetType("profView.init"));
	profview.result = profview_main:main();
end