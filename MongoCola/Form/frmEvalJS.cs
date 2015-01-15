﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Common;
using MongoUtility.Operation;
using MongoDB.Bson;
using MongoDB.Driver;
using SystemUtility;
using ResourceLib;

namespace MongoCola
{
    public partial class frmEvalJS : Form
    {
        public frmEvalJS()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmevalJS_Load(object sender, EventArgs e)
        {
            if (!SystemConfig.IsUseDefaultLanguage)
            {
                Text = SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.EvalJS_Title);
                ctlEval.Title = SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.EvalJS_Method);
                lblParm.Text = SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.EvalJS_Parameter);
                cmdEval.Text = SystemConfig.guiConfig.MStringResource.GetText(StringResource.TextType.EvalJS_Run);
            }
            ctlEval.Context =
                @"function eval(){
var i = 0;
i++;
return i;
}";
        }

        /// <summary>
        ///     eval Javascript
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdEval_Click(object sender, EventArgs e)
        {
            MongoDatabase mongoDB = MongoUtility.Core.RuntimeMongoDBContext.GetCurrentDataBase();
            var js = new BsonJavaScript(ctlEval.Context);
            var Params = new List<Object>();
            if (txtParm.Text != String.Empty)
            {
                foreach (String parm in txtParm.Text.Split(",".ToCharArray()))
                {
                    if (parm.StartsWith("'") & parm.EndsWith("'"))
                    {
                        Params.Add(parm.Trim("'".ToCharArray()));
                    }
                    else
                    {
                        try
                        {
                            Boolean isNuberic = true;
                            for (int i = 0; i < parm.Length; i++)
                            {
                                if (!char.IsNumber(parm, i))
                                {
                                    isNuberic = false;
                                }
                            }
                            if (isNuberic)
                            {
                                Params.Add(Convert.ToInt16(parm));
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.Utility.ExceptionDeal(ex, "Exception", "Parameter Exception");
                        }
                    }
                }
            }
            try
            {
                BsonValue result = mongoDB.Eval(js, Params.ToArray());
                MyMessageBox.ShowMessage("Result", "Result", result.ToJson(MongoUtility.Basic.Utility.JsonWriterSettings), true);
            }
            catch (Exception ex)
            {
                Common.Utility.ExceptionDeal(ex, "Exception", "Result");
            }
        }
    }
}