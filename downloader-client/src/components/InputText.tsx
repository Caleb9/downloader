interface IInputTextProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
}

const InputText = (props: IInputTextProps) => (
  <>
    <label>{props.label}</label>
    <input
      type="text"
      value={props.value}
      onChange={(event) => props.onChange(event.target.value)}
    />
  </>
);

export default InputText;
