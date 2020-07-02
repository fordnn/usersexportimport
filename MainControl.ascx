<%@ Control Language="C#" Inherits="forDNN.Modules.UsersExportImport.MainControl" AutoEventWireup="true"
    CodeBehind="MainControl.ascx.cs" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<script type="text/javascript">
    function importProfileProperties(ctl1, ctl2)
    {
        if ($(ctl1).is(":checked"))
        {
            $(ctl2).prop('disabled', false);
        }
        else
        {
            $(ctl2).prop('disabled', true);
            $(ctl2).prop('checked', false);
        }
    }
</script>

<div class="dnnForm" id="tabs-demo">
    <ul class="dnnAdminTabNav">
        <li><a href="#divExportUsers">
            <asp:Label runat="server" ID="lblExportUsers" resourcekey="ExportUsers"></asp:Label></a></li>
        <li><a href="#divImportUsers">
            <asp:Label runat="server" ID="lblImportUsers" resourcekey="ImportUsers"></asp:Label></a></li>
    </ul>
    <div id="divExportUsers" class="dnnClear dnnForm">
        <fieldset>
            <div class="dnnFormItem">
                <dnn:Label runat="server" ID="lblExportFileType" resourcekey="ExportFileType"></dnn:Label>
                <asp:DropDownList runat="server" ID="ddlExportFileType" Style="width: 100px;">
                    <asp:ListItem Value="1" resourcekey="ExportFileType_1"></asp:ListItem>
                    <asp:ListItem Value="2" resourcekey="ExportFileType_2" Selected></asp:ListItem>
                </asp:DropDownList>
            </div>
            <div class="dnnFormItem" runat="server" id="divIncludeSuperUsers">
                <dnn:Label runat="server" id="lblIncludeSuperUsers" resourcekey="IncludeSuperUsers" AssociatedControlID="cbIncludeSuperUsers"></dnn:Label>
                <asp:CheckBox runat="server" ID="cbIncludeSuperUsers" CssClass="normalCheckBox" />
            </div>
            <div class="dnnFormItem">
                <dnn:Label runat="server" id="lblIncludeDeleted" resourcekey="IncludeDeleted" AssociatedControlID="cbIncludeDeleted"></dnn:Label>
                <asp:CheckBox runat="server" ID="cbIncludeDeleted" CssClass="normalCheckBox" />
            </div>
            <div class="dnnFormItem">
                <dnn:Label runat="server" id="lblIncludeNonAuthorised" resourcekey="IncludeNonAuthorised" AssociatedControlID="cbIncludeNonAuthorised"></dnn:Label>
                <asp:CheckBox runat="server" ID="cbIncludeNonAuthorised" CssClass="normalCheckBox" />
            </div>
            <div class="dnnFormItem">
                <dnn:Label runat="server" id="lblExportRoles" resourcekey="ExportRoles" AssociatedControlID="cbExportRoles"></dnn:Label>
                <asp:CheckBox runat="server" ID="cbExportRoles" CssClass="normalCheckBox" />
            </div>
            <div class="dnnFormItem">
                <dnn:Label runat="server" id="lblExportPasswords" resourcekey="ExportPasswords" AssociatedControlID="cbExportPasswords"></dnn:Label>
                <asp:CheckBox runat="server" ID="cbExportPasswords" CssClass="normalCheckBox" />
            </div>
            <div>
                <asp:Label runat="server" ID="lblExportPasswordsDisabled" resourcekey="ExportPasswordsDisabled" CssClass="NormalRed" Visible="false"></asp:Label>
            </div>
            <div class="dnnFormItem">
                <dnn:Label runat="server" ID="lblPropertiesToExport" resourcekey="PropertiesToExport"></dnn:Label>
                <asp:CheckBox runat="server" ID="cbPropertiesToExport" CssClass="SubHead normalCheckBox" />
            </div>
            <div id="divPropertiesToExport">
                <asp:CheckBoxList runat="server" ID="cblPropertiesToExport" RepeatDirection="Vertical" RepeatColumns="3" CssClass="normalCheckBox"></asp:CheckBoxList>
            </div>
        </fieldset>
		<asp:HyperLink ID="btnExportUsers" runat="server" CssClass="dnnPrimaryAction" resourcekey="ExportUsers"></asp:HyperLink>
		<div id="ExportInProgress" style="display:none;">
			<asp:Label runat="server" ID="lblExportInProgress" resourcekey="ExportInProgress" CssClass="NormalRed"></asp:Label>
		</div>
    </div>
    <div id="divImportUsers" class="dnnClear dnnForm">
        <fieldset>
            <div class="dnnFormItem">
                <dnn:Label ID="lblImport" runat="server" resourcekey="Import"></dnn:Label>
                <input id="objFile" type="file" name="objFile" runat="server">
            </div>
            <div>
                <asp:Label ID="lblMaxAllowedFileSize" runat="server" CssClass="NormalRed"></asp:Label>
            </div>
            <div class="dnnFormItem">
                <dnn:Label ID="lblImportRoles" runat="server" resourcekey="ImportRoles"></dnn:Label>
                <asp:RadioButtonList runat="server" ID="rblImportRoles">
                    <asp:ListItem Value="0" resourcekey="ImportRoles0" Selected></asp:ListItem>
                    <asp:ListItem Value="1" resourcekey="ImportRoles1"></asp:ListItem>
                    <asp:ListItem Value="3" resourcekey="ImportRoles3"></asp:ListItem>
                    <asp:ListItem Value="2" resourcekey="ImportRoles2"></asp:ListItem>
                </asp:RadioButtonList>
            </div>
            <div class="dnnFormItem">
                <dnn:Label ID="lblImportProfileProperties" runat="server" resourcekey="ImportProfileProperties"></dnn:Label>
                <asp:CheckBox runat="server" ID="cbImportProfileProperties" CssClass="normalCheckBox" Checked="true" />
            </div>
            <div class="dnnFormItem">
                <dnn:Label ID="lblCreateMissedProfileProperties" runat="server" resourcekey="CreateMissedProfileProperties"></dnn:Label>
                <asp:CheckBox runat="server" ID="cbCreateMissedProfileProperties" CssClass="normalCheckBox" />
            </div>
            <div class="dnnFormItem">
                <dnn:Label ID="lblRandomPassword" runat="server" resourcekey="RandomPassword"></dnn:Label>
                <asp:CheckBox runat="server" ID="cbRandomPassword" CssClass="normalCheckBox" />
            </div>
            <div class="dnnFormItem">
                <dnn:Label ID="lblForcePasswordChange" runat="server" resourcekey="ForcePasswordChange"></dnn:Label>
                <asp:CheckBox runat="server" ID="cbForcePasswordChange" CssClass="normalCheckBox" />
            </div>
            <div class="dnnFormItem">
                <dnn:Label ID="lblEmailUser" runat="server" resourcekey="EmailUser"></dnn:Label>
                <asp:CheckBox runat="server" ID="cbEmailUser" CssClass="normalCheckBox" />
            </div>
        </fieldset>
        <div style="clear: both;">
            <asp:LinkButton ID="btnImport" runat="server" CssClass="dnnPrimaryAction" resourcekey="ImportUsers"></asp:LinkButton>
        </div>
        <fieldset>
            <div class="dnnFormItem">
                <dnn:Label runat="server" ID="lblExamples" resourcekey="Examples"></dnn:Label>
                <asp:HyperLink runat="server" ID="lnkExampleCSV" resourcekey="ExampleCSV" Target="_blank"></asp:HyperLink><br />
                <asp:HyperLink runat="server" ID="lnkExampleXML" resourcekey="ExampleXML" Target="_blank"></asp:HyperLink>
            </div>
        </fieldset>
    </div>
</div>

<div>
    <asp:Label ID="lblResult" CssClass="NormalRed" runat="server"></asp:Label>
</div>
<div style="padding-top: 100px;">
    <asp:Label ID="lblIcon" runat="server"></asp:Label>
</div>

<script type="text/javascript">
	$(document).ready(function ()
	{
		$('#tabs-demo').dnnTabs();
	});

	function doExport(moduleId)
	{
		var objValues = new Object();
		objValues.ExportFileType = $("#<%=ddlExportFileType.ClientID%>").val();
		objValues.IncludeSuperUsers = $("#<%=cbIncludeSuperUsers.ClientID%>").prop("checked");
		objValues.IncludeDeleted = $("#<%=cbIncludeDeleted.ClientID%>").prop("checked");
		objValues.IncludeNonAuthorised = $("#<%=cbIncludeNonAuthorised.ClientID%>").prop("checked");
		objValues.ExportRoles = $("#<%=cbExportRoles.ClientID%>").prop("checked");
		objValues.ExportPasswords = $("#<%=cbExportPasswords.ClientID%>").prop("checked");
		objValues.PropertiesToExport = "";
		$("#<%=cblPropertiesToExport.ClientID%> input[type='checkbox']:checked").each(
			function (ind, val)
			{
				var objVal = $(val);
				objValues.PropertiesToExport += ((objValues.PropertiesToExport==="")?"":",") + objVal.parent().find("label").html() + "=" + objVal.val();
			}
		);

		$("#<%=btnExportUsers.ClientID%>").toggle();
		$("#ExportInProgress").toggle();

		callWebAPI(moduleId, "DoExport", objValues, function (data)
		{
			$("#<%=btnExportUsers.ClientID%>").toggle();
			$("#ExportInProgress").toggle();

			if (data === "")
			{
				alert("Export error, please check logs");
			}
			else
			{
				document.location.href = data;
			}
		});
		return false;
	}

	function doImport(moduleId)
	{
		var formData = new FormData();

		var objItem = $("#<%=objFile.ClientID%>");
		if (objItem[0].files.length == 0)
		{
			return false; //go to the next file
		}
		var files = objItem[0].files;
		var newName = "";
		for (var i = 0; i < files.length; i++)
		{
			newName = files[i].name;
			formData.append(newName, files[i]);
		}

		formData.append('rblImportRoles',					$('[name="dnn$ctr' + moduleId + '$MainControl$rblImportRoles"]:checked').val());
		formData.append('cbImportProfileProperties',		$("#<%=cbImportProfileProperties.ClientID%>").is(":checked"));
		formData.append('cbCreateMissedProfileProperties',	$("#<%=cbCreateMissedProfileProperties.ClientID%>").is(":checked"));
		formData.append('cbRandomPassword',					$("#<%=cbRandomPassword.ClientID%>").is(":checked"));
		formData.append('cbForcePasswordChange',			$("#<%=cbForcePasswordChange.ClientID%>").is(":checked"));	
		formData.append('cbEmailUser',						$("#<%=cbEmailUser.ClientID%>").is(":checked"));

		var sf = $.ServicesFramework(moduleId);
		var serviceUrl = sf.getServiceRoot('forDNN.UsersExportImport') + "ExportImport/DoImport";
		$.ajax({
			type: "POST",
			url: serviceUrl,
			data: formData,
			contentType: false,
			headers: { "RequestVerificationToken": sf.getAntiForgeryValue(), "ModuleId": sf.getModuleId(), "TabId": sf.getTabId() },
			processData: false,
			cache: false,
			async: false,
			success: function (data, status, xhr)
			{
				$("#<%=lblResult.ClientID%>").html(data);
			}
		});
		return false;
	}

	function callWebAPI(moduleId, methodName, values, callBack)
	{
		var sf = $.ServicesFramework(moduleId);
		var serviceUrl = sf.getServiceRoot('forDNN.UsersExportImport') + "ExportImport/" + methodName;

		$.ajax({
			url: serviceUrl,
			data: JSON.stringify(values),
			type: "POST",
			contentType: "application/json",
			headers: { "RequestVerificationToken": sf.getAntiForgeryValue(), "ModuleId": sf.getModuleId(), "TabId": sf.getTabId() },
			success: function (data, status, xhr)
			{
				callBack(data);
			},
			error: function (xhr, status, err)
			{
				console.error(serviceUrl, status, err.toString());
			}
		});
	}
</script>
 