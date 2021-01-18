interface Props {
  label: string;
  "data-testid": string;
  value: string;
  onChange: (value: string) => void;
}

export default function InputText(props: Props) {
  return (
    <>
      <label>{props.label}</label>
      <input
        type="text"
        data-testid={props["data-testid"]}
        value={props.value}
        onChange={(event) => props.onChange(event.target.value)}
      />
    </>
  );
}
