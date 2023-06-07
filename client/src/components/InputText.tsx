import "./InputText.css";

interface Props {
  label: string;
  title: string;
  "data-testid": string;
  value: string;
  onChange: (value: string) => void;
  isValid: boolean;
}

export default function InputText(props: Props) {
  const getClass = (isValid: boolean): string => {
    switch (isValid) {
      case true:
        return "InputText-Valid";
      case false:
        return "InputText-Invalid";
    }
  };

  return (
    <>
      <label>{props.label}</label>
      <input
        type="text"
        data-testid={props["data-testid"]}
        value={props.value}
        onChange={(event) => props.onChange(event.target.value)}
        className={getClass(props.isValid)}
        title={props.title}
      />
    </>
  );
}
