// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 2.0.50727.42
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace MonoDevelop.Projects.Gui.Deployment {
    
    
    public partial class FileCopyConfigurationSelector {
        
        private Gtk.VBox vbox1;
        
        private Gtk.HBox hbox1;
        
        private Gtk.Label label1;
        
        private Gtk.ComboBox comboHandlers;
        
        private Gtk.HSeparator hseparator1;
        
        private Gtk.EventBox editorBox;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize();
            // Widget MonoDevelop.Projects.Gui.Deployment.FileCopyConfigurationSelector
            Stetic.BinContainer.Attach(this);
            this.Events = ((Gdk.EventMask)(256));
            this.Name = "MonoDevelop.Projects.Gui.Deployment.FileCopyConfigurationSelector";
            // Container child MonoDevelop.Projects.Gui.Deployment.FileCopyConfigurationSelector.Gtk.Container+ContainerChild
            this.vbox1 = new Gtk.VBox();
            this.vbox1.Name = "vbox1";
            this.vbox1.Spacing = 6;
            // Container child vbox1.Gtk.Box+BoxChild
            this.hbox1 = new Gtk.HBox();
            this.hbox1.Name = "hbox1";
            this.hbox1.Spacing = 6;
            // Container child hbox1.Gtk.Box+BoxChild
            this.label1 = new Gtk.Label();
            this.label1.Name = "label1";
            this.label1.Xalign = 0F;
            this.label1.LabelProp = MonoDevelop.Core.GettextCatalog.GetString("Target:");
            this.hbox1.Add(this.label1);
            Gtk.Box.BoxChild w1 = ((Gtk.Box.BoxChild)(this.hbox1[this.label1]));
            w1.Position = 0;
            w1.Expand = false;
            w1.Fill = false;
            // Container child hbox1.Gtk.Box+BoxChild
            this.comboHandlers = Gtk.ComboBox.NewText();
            this.comboHandlers.Name = "comboHandlers";
            this.hbox1.Add(this.comboHandlers);
            Gtk.Box.BoxChild w2 = ((Gtk.Box.BoxChild)(this.hbox1[this.comboHandlers]));
            w2.Position = 1;
            this.vbox1.Add(this.hbox1);
            Gtk.Box.BoxChild w3 = ((Gtk.Box.BoxChild)(this.vbox1[this.hbox1]));
            w3.Position = 0;
            w3.Expand = false;
            w3.Fill = false;
            // Container child vbox1.Gtk.Box+BoxChild
            this.hseparator1 = new Gtk.HSeparator();
            this.hseparator1.Name = "hseparator1";
            this.vbox1.Add(this.hseparator1);
            Gtk.Box.BoxChild w4 = ((Gtk.Box.BoxChild)(this.vbox1[this.hseparator1]));
            w4.Position = 1;
            w4.Expand = false;
            w4.Fill = false;
            // Container child vbox1.Gtk.Box+BoxChild
            this.editorBox = new Gtk.EventBox();
            this.editorBox.Name = "editorBox";
            this.vbox1.Add(this.editorBox);
            Gtk.Box.BoxChild w5 = ((Gtk.Box.BoxChild)(this.vbox1[this.editorBox]));
            w5.Position = 2;
            this.Add(this.vbox1);
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.Show();
            this.comboHandlers.Changed += new System.EventHandler(this.OnComboHandlersChanged);
        }
    }
}
