<%@ Control Language="C#" Inherits="forDNN.Modules.UsersExportImport.MainControl" AutoEventWireup="true"
    CodeBehind="MainControl.ascx.cs" %>

<script type="text/javascript">
    function importProfileProperties(ctl1, ctl2) {
        if ($(ctl1).is(":checked")) {
            $(ctl2).prop('disabled', false);
        }
        else {
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
    <div id="divExportUsers" class="dnnClear">
        <div>
            <asp:Label runat="server" ID="lblExportFileType" resourcekey="ExportFileType" CssClass="SubHead"></asp:Label>
            <asp:DropDownList runat="server" ID="ddlExportFileType">
                <asp:ListItem Value="1" resourcekey="ExportFileType_1"></asp:ListItem>
                <asp:ListItem Value="2" resourcekey="ExportFileType_2" Selected></asp:ListItem>
            </asp:DropDownList>
        </div>
        <div>
            <asp:CheckBox runat="server" ID="cbIncludeSuperUsers" resourcekey="IncludeSuperUsers" CssClass="SubHead normalCheckBox" />
        </div>
        <div>
            <asp:CheckBox runat="server" ID="cbIncludeDeleted" resourcekey="IncludeDeleted" CssClass="SubHead normalCheckBox" />
        </div>
        <div>
            <asp:CheckBox runat="server" ID="cbIncludeNonAuthorised" resourcekey="IncludeNonAuthorised" CssClass="SubHead normalCheckBox" />
        </div>
        <div>
            <asp:CheckBox runat="server" ID="cbExportRoles" resourcekey="ExportRoles" CssClass="SubHead normalCheckBox" />
        </div>
        <div>
            <asp:CheckBox runat="server" ID="cbExportPasswords" CssClass="SubHead normalCheckBox" />
        </div>
        <div>
            <asp:Label runat="server" ID="lblPropertiesToExport" resourcekey="PropertiesToExport" CssClass="SubHead"></asp:Label>
            <asp:CheckBox runat="server" ID="cbPropertiesToExport" CssClass="SubHead normalCheckBox" />
            <div id="divPropertiesToExport">
                <asp:CheckBoxList runat="server" ID="cblPropertiesToExport" RepeatDirection="Vertical" RepeatColumns="3" CssClass="normalCheckBox"></asp:CheckBoxList>
            </div>
        </div>
        <asp:LinkButton ID="btnExportUsers" runat="server" CssClass="dnnPrimaryAction" OnClick="btnExportUsers_Click"
            resourcekey="ExportUsers"></asp:LinkButton>
    </div>
    <div id="divImportUsers" class="dnnClear">
        <div>
            <asp:Label ID="lblImport" CssClass="NormalRed" runat="server" resourcekey="Import"></asp:Label>&nbsp;
    <input id="objFile" type="file" name="objFile" runat="server">
        </div>
        <div>
            <asp:RadioButtonList runat="server" ID="rblImportRoles">
                <asp:ListItem Value="0" resourcekey="ImportRoles0" Selected></asp:ListItem>
                <asp:ListItem Value="1" resourcekey="ImportRoles1"></asp:ListItem>
                <asp:ListItem Value="2" resourcekey="ImportRoles2"></asp:ListItem>
            </asp:RadioButtonList>
        </div>
        <div>
            <asp:CheckBox runat="server" ID="cbImportProfileProperties" resourcekey="ImportProfileProperties" CssClass="SubHead normalCheckBox" Checked="true" />
        </div>
        <div>
            <asp:CheckBox runat="server" ID="cbCreateMissedProfileProperties" resourcekey="CreateMissedProfileProperties" CssClass="SubHead normalCheckBox" />
        </div>
        <div style="clear: both;">
            <asp:LinkButton ID="btnImport" runat="server" CssClass="dnnPrimaryAction" OnClick="btnImport_Click"
                resourcekey="ImportUsers"></asp:LinkButton>
        </div>
    </div>
</div>

<div>
    <asp:Label ID="lblResult" CssClass="NormalRed" runat="server"></asp:Label>
</div>
<div style="padding-top: 100px;">
    <asp:Label ID="lblIcon" runat="server"></asp:Label>
</div>

<script type="text/javascript">
    jQuery(function ($) {
        $('#tabs-demo').dnnTabs();
    });
</script>
