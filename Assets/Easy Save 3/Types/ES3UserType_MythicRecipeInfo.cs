using System;
using UnityEngine;

namespace ES3Types
{
	[UnityEngine.Scripting.Preserve]
	[ES3PropertiesAttribute("isFirstUnlock", "isSecondUnlock", "isThirdUnlock", "isFourthUnlock", "isFifthUnlock")]
	public class ES3UserType_MythicRecipeInfo : ES3ObjectType
	{
		public static ES3Type Instance = null;

		public ES3UserType_MythicRecipeInfo() : base(typeof(MythicRecipeInfo)){ Instance = this; priority = 1; }


		protected override void WriteObject(object obj, ES3Writer writer)
		{
			var instance = (MythicRecipeInfo)obj;
			
			writer.WriteProperty("isFirstUnlock", instance.isFirstUnlock, ES3Type_bool.Instance);
			writer.WriteProperty("isSecondUnlock", instance.isSecondUnlock, ES3Type_bool.Instance);
			writer.WriteProperty("isThirdUnlock", instance.isThirdUnlock, ES3Type_bool.Instance);
			writer.WriteProperty("isFourthUnlock", instance.isFourthUnlock, ES3Type_bool.Instance);
			writer.WriteProperty("isFifthUnlock", instance.isFifthUnlock, ES3Type_bool.Instance);
		}

		protected override void ReadObject<T>(ES3Reader reader, object obj)
		{
			var instance = (MythicRecipeInfo)obj;
			foreach(string propertyName in reader.Properties)
			{
				switch(propertyName)
				{
					
					case "isFirstUnlock":
						instance.isFirstUnlock = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					case "isSecondUnlock":
						instance.isSecondUnlock = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					case "isThirdUnlock":
						instance.isThirdUnlock = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					case "isFourthUnlock":
						instance.isFourthUnlock = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					case "isFifthUnlock":
						instance.isFifthUnlock = reader.Read<System.Boolean>(ES3Type_bool.Instance);
						break;
					default:
						reader.Skip();
						break;
				}
			}
		}

		protected override object ReadObject<T>(ES3Reader reader)
		{
			var instance = new MythicRecipeInfo();
			ReadObject<T>(reader, instance);
			return instance;
		}
	}


	public class ES3UserType_MythicRecipeInfoArray : ES3ArrayType
	{
		public static ES3Type Instance;

		public ES3UserType_MythicRecipeInfoArray() : base(typeof(MythicRecipeInfo[]), ES3UserType_MythicRecipeInfo.Instance)
		{
			Instance = this;
		}
	}
}