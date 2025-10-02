using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("Id", "Sequence", "ResultUnit", "OutputCount", "UnlockMessage", "unlockWave")]
	public class ES3UserType_MythicRecipe : ES3ScriptableObjectType
	{
		public static ES3Type Instance = null;

		public ES3UserType_MythicRecipe() : base(typeof(MythicRecipe)){ Instance = this; priority = 1; }


		protected override void WriteScriptableObject(object obj, ES3Writer writer)
		{
			var instance = (MythicRecipe)obj;
			
			writer.WriteProperty("Id", instance.Id, ES3Type_string.Instance);
			writer.WriteProperty("Sequence", instance.Sequence, ES3Internal.ES3TypeMgr.GetOrCreateES3Type(typeof(System.Collections.Generic.List<UnitData>)));
			writer.WritePropertyByRef("ResultUnit", instance.ResultUnit);
			writer.WriteProperty("OutputCount", instance.OutputCount, ES3Type_int.Instance);
			writer.WriteProperty("UnlockMessage", instance.UnlockMessage, ES3Type_string.Instance);
			writer.WriteProperty("unlockWave", instance.unlockWave, ES3Type_int.Instance);
		}

		protected override void ReadScriptableObject<T>(ES3Reader reader, object obj)
		{
			var instance = (MythicRecipe)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "Id":
						instance.Id = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "Sequence":
						instance.Sequence = reader.Read<System.Collections.Generic.List<UnitData>>();
						break;
					case "ResultUnit":
						instance.ResultUnit = reader.Read<UnitData>();
						break;
					case "OutputCount":
						instance.OutputCount = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					case "UnlockMessage":
						instance.UnlockMessage = reader.Read<System.String>(ES3Type_string.Instance);
						break;
					case "unlockWave":
						instance.unlockWave = reader.Read<System.Int32>(ES3Type_int.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}
	}


	public class ES3UserType_MythicRecipeArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_MythicRecipeArray() : base(typeof(MythicRecipe[]), ES3UserType_MythicRecipe.Instance)
		{
			Instance = this;
		}
	}
}